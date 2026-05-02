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
3. Ajustar `appsettings` e secrets.
4. Criar migrations iniciais.
5. Substituir os stubs de Google/Stripe pelas integrações reais.
