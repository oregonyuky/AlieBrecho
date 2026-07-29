using System.Net;
using System.Net.Http.Headers;
using Application.Common.Repositories;
using Application.Common.Services.FileImageManager;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.FileImageManager;

public class FileImageService : IFileImageService
{
    private static readonly IReadOnlyDictionary<string, string> AllowedTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = "jpg",
            ["image/png"] = "png",
            ["image/webp"] = "webp"
        };

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommandRepository<FileImage> _imageRepository;
    private readonly HttpClient _httpClient;
    private readonly ILogger<FileImageService> _logger;
    private readonly string _legacyFolderPath;
    private readonly string _supabaseUrl;
    private readonly string _serviceRoleKey;
    private readonly string _bucket;
    private readonly int _maxFileSizeInBytes;

    public FileImageService(
        IUnitOfWork unitOfWork,
        IOptions<FileImageSettings> settings,
        ICommandRepository<FileImage> imageRepository,
        HttpClient httpClient,
        ILogger<FileImageService> logger)
    {
        var options = settings.Value;
        _unitOfWork = unitOfWork;
        _imageRepository = imageRepository;
        _httpClient = httpClient;
        _logger = logger;
        _legacyFolderPath = Path.Combine(Directory.GetCurrentDirectory(), options.PathFolder);
        _maxFileSizeInBytes = options.MaxFileSizeInMB * 1024 * 1024;
        _supabaseUrl = options.SupabaseUrl.Trim().TrimEnd('/');
        _serviceRoleKey = options.SupabaseServiceRoleKey.Trim();
        _bucket = string.IsNullOrWhiteSpace(options.SupabaseStorageBucket)
            ? "products"
            : options.SupabaseStorageBucket.Trim();
    }

    public async Task<string> UploadAsync(
        string? originalFileName,
        string? docExtension,
        byte[]? fileData,
        long? size,
        string? contentType,
        string? description = "",
        string? createdById = "",
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        if (fileData is null || fileData.Length == 0)
            throw new InvalidOperationException("A imagem enviada está vazia.");

        if (fileData.Length > _maxFileSizeInBytes)
            throw new InvalidOperationException(
                $"A imagem deve ter no máximo {_maxFileSizeInBytes / (1024 * 1024)} MB.");

        if (string.IsNullOrWhiteSpace(contentType) ||
            !AllowedTypes.TryGetValue(contentType, out var normalizedExtension))
            throw new InvalidOperationException("Formato inválido. Envie uma imagem JPEG, PNG ou WebP.");

        var suppliedExtension = docExtension?.Trim().TrimStart('.').ToLowerInvariant();
        if (suppliedExtension == "jpeg")
            suppliedExtension = "jpg";
        if (!string.Equals(suppliedExtension, normalizedExtension, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A extensão do arquivo não corresponde ao tipo da imagem.");

        var objectPath = $"pending/{Guid.NewGuid():N}.{normalizedExtension}";
        var objectEndpoint = BuildObjectEndpoint(objectPath);

        using var content = new ByteArrayContent(fileData);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        using var request = new HttpRequestMessage(HttpMethod.Post, objectEndpoint)
        {
            Content = content
        };
        AddAuthorizationHeaders(request);
        request.Headers.TryAddWithoutValidation("x-upsert", "false");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Supabase Storage recusou upload de imagem. Status: {StatusCode}. Resposta: {Response}",
                    (int)response.StatusCode,
                    Truncate(responseBody, 500));
                throw new InvalidOperationException(
                    "Não foi possível armazenar a imagem. Tente novamente mais tarde.");
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Timeout ao enviar imagem para o Supabase Storage.");
            throw new InvalidOperationException("O armazenamento de imagens demorou para responder.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha de comunicação com o Supabase Storage.");
            throw new InvalidOperationException(
                "O armazenamento de imagens está indisponível. Tente novamente mais tarde.");
        }

        var publicUrl = BuildPublicUrl(objectPath);
        var image = new FileImage
        {
            Name = publicUrl,
            OriginalName = originalFileName,
            Extension = normalizedExtension,
            GeneratedName = objectPath,
            FileSize = size,
            Description = description,
            CreatedById = createdById
        };

        try
        {
            await _imageRepository.CreateAsync(image, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);
        }
        catch
        {
            await TryDeleteObjectAsync(objectPath, cancellationToken);
            throw;
        }

        return publicUrl;
    }

    public async Task<byte[]> GetFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (Uri.TryCreate(fileName, UriKind.Absolute, out var remoteUri) &&
            (remoteUri.Scheme == Uri.UriSchemeHttps || remoteUri.Scheme == Uri.UriSchemeHttp))
        {
            try
            {
                return await _httpClient.GetByteArrayAsync(remoteUri, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Não foi possível obter imagem remota.");
            }
        }
        else
        {
            var safeFileName = Path.GetFileName(fileName);
            var legacyPath = Path.Combine(_legacyFolderPath, safeFileName);
            if (File.Exists(legacyPath))
                return await File.ReadAllBytesAsync(legacyPath, cancellationToken);
        }

        var fallbackPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "noimage.png");
        return await File.ReadAllBytesAsync(fallbackPath, cancellationToken);
    }

    public async Task DeleteAsync(string? fileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        var objectPath = TryGetOwnedObjectPath(fileName);
        if (objectPath is not null)
        {
            await TryDeleteObjectAsync(objectPath, cancellationToken);
        }
        else if (!Uri.TryCreate(fileName, UriKind.Absolute, out _))
        {
            var safeFileName = Path.GetFileName(fileName);
            var legacyPath = Path.Combine(_legacyFolderPath, safeFileName);
            if (File.Exists(legacyPath))
                File.Delete(legacyPath);
        }

        var image = _imageRepository.GetQuery()
            .FirstOrDefault(x => x.GeneratedName == objectPath ||
                                 x.GeneratedName == fileName ||
                                 x.Name == fileName);
        if (image is not null)
        {
            _imageRepository.Delete(image);
            await _unitOfWork.SaveAsync(cancellationToken);
        }
    }

    private async Task TryDeleteObjectAsync(string objectPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_supabaseUrl) || string.IsNullOrWhiteSpace(_serviceRoleKey))
            return;

        using var request = new HttpRequestMessage(HttpMethod.Delete, BuildObjectEndpoint(objectPath));
        AddAuthorizationHeaders(request);
        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
            {
                _logger.LogWarning(
                    "Não foi possível excluir objeto do Supabase Storage. Status: {StatusCode}",
                    (int)response.StatusCode);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Falha ao excluir objeto do Supabase Storage.");
        }
    }

    private string? TryGetOwnedObjectPath(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return null;

        if (!Uri.TryCreate(_supabaseUrl, UriKind.Absolute, out var configuredBase) ||
            !string.Equals(uri.Host, configuredBase.Host, StringComparison.OrdinalIgnoreCase))
            return null;

        var marker = $"/storage/v1/object/public/{Uri.EscapeDataString(_bucket)}/";
        var index = uri.AbsolutePath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return index < 0 ? null : Uri.UnescapeDataString(uri.AbsolutePath[(index + marker.Length)..]);
    }

    private string BuildObjectEndpoint(string objectPath) =>
        $"{_supabaseUrl}/storage/v1/object/{Uri.EscapeDataString(_bucket)}/{EscapePath(objectPath)}";

    private string BuildPublicUrl(string objectPath) =>
        $"{_supabaseUrl}/storage/v1/object/public/{Uri.EscapeDataString(_bucket)}/{EscapePath(objectPath)}";

    private static string EscapePath(string path) =>
        string.Join('/', path.Split('/').Select(Uri.EscapeDataString));

    private void AddAuthorizationHeaders(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation("apikey", _serviceRoleKey);

        // New sb_secret_ keys are API keys, not JWTs, and must not be sent as Bearer tokens.
        // Legacy service_role keys are JWTs and Storage uses them for RLS bypass.
        if (!_serviceRoleKey.StartsWith("sb_secret_", StringComparison.OrdinalIgnoreCase))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private void EnsureConfigured()
    {
        if (!Uri.TryCreate(_supabaseUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(_serviceRoleKey))
        {
            _logger.LogError("Configuração do Supabase Storage ausente ou inválida.");
            throw new InvalidOperationException(
                "O armazenamento de imagens não está configurado no servidor.");
        }
    }
}
