# Deploy do AlieBrecho no Railway

O projeto usa o `Dockerfile` da raiz, escuta a porta fornecida pelo Railway e converte `DATABASE_URL` automaticamente para a configuração PostgreSQL do Entity Framework Core.

## Serviços

1. Crie um projeto no Railway e adicione um banco PostgreSQL.
2. Crie o serviço da API a partir deste repositório.
3. No serviço da API, defina as variáveis abaixo.
4. Gere um domínio público em **Settings > Networking** se o painel administrativo ou webhooks precisarem acessar a API pela internet.
5. Anexe um volume em `/app/wwwroot/app_data` para preservar uploads e chaves criptográficas.

Mantenha uma única réplica deste serviço: o startup atualiza o esquema do banco e o volume de uploads é exclusivo da instância.

## Variáveis obrigatórias

```text
PORT=8080
DATABASE_URL=${{Postgres.DATABASE_URL}}
DatabaseProvider=PostgreSQL
Jwt__Key=<segredo-aleatorio-com-pelo-menos-32-caracteres>
Jwt__Issuer=AlieBrecho
Jwt__Audience=AlieBrecho
AspNetIdentity__DefaultAdmin__Email=<email-do-admin>
AspNetIdentity__DefaultAdmin__Password=<senha-forte>
AspNetIdentity__DefaultAdmin__PostCode=<cep-da-loja>
IsDemoVersion=false
```

O nome `Postgres` na referência deve ser igual ao nome do serviço de banco no Railway. `DatabaseProvider` é explicitado por clareza, embora a presença de `DATABASE_URL` já selecione PostgreSQL no aplicativo.

## Integrações opcionais

Configure apenas as integrações usadas:

```text
MELHOR_ENVIO_TOKEN=...
MERCADO_PAGO_ACCESS_TOKEN=...
INFINITE_PAY_HANDLE=...
INFINITE_PAY_REDIRECT_URL=https://<dominio-front>/obrigado
INFINITE_PAY_WEBHOOK_URL=https://<dominio-api>/<rota-do-webhook>
```

O health check `/healthz` já está configurado em `railway.json`. O startup cria/atualiza a estrutura do banco antes de começar a responder, portanto o primeiro deploy pode levar mais tempo.

## Ligação com o front

Mantenha API e front no mesmo projeto Railway. No serviço do front, use:

```text
AlieBrechoApi__BaseUrl=http://${{api.RAILWAY_PRIVATE_DOMAIN}}:${{api.PORT}}/
```

Substitua `api` pelo nome exato deste serviço. A comunicação interna usa HTTP; o TLS público é terminado pelo Railway.
