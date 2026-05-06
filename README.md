# Diguifi Studios Backend

Esqueleto inicial do backend em `.NET 10` com arquitetura em camadas para autenticação, catálogo, checkout Stripe e webhooks.

## Estrutura

- `src/Diguifi.Api`: camada HTTP, Swagger, middleware e autenticação.
- `src/Diguifi.Application`: DTOs, interfaces e contratos de caso de uso.
- `src/Diguifi.Domain`: entidades e enums do domínio.
- `src/Diguifi.Infrastructure`: EF Core, serviços de token, integrações e seed inicial.
- `tests`: suites unitária e de integração.

## Próximos passos

1. Instalar o SDK `.NET 10`.
2. Restaurar dependências com `dotnet restore`.
3. Configurar variáveis de ambiente.
4. Criar migrations iniciais.
5. Substituir os stubs de Google/Stripe pelas integrações reais.

## Configuração

O projeto lê configuração nesta ordem:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. `.env` local em `src/Diguifi.Api/.env` quando `ASPNETCORE_ENVIRONMENT=Development`
4. variáveis de ambiente reais do sistema/plataforma
5. argumentos de linha de comando

Isso permite usar o mesmo formato em desenvolvimento e produção. Para valores aninhados, use `__` no nome da variável.

Exemplos:

- `ConnectionStrings__Default`
- `Jwt__SigningKey`
- `Google__ClientSecret`
- `Stripe__WebhookSecret`
- `Stripe__CliWebhookSecret`
- `Frontend__BaseUrl`

Para desenvolvimento local, use [src/Diguifi.Api/.env.example](/C:/Users/Casal%20Mozis/Desktop/Repos/diguifistudios-backend/src/Diguifi.Api/.env.example) como base e crie `src/Diguifi.Api/.env`.

Em produção, configure essas mesmas chaves diretamente no Render.
