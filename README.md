
# Desafio Banco Digital

Implementação de um sistema bancário simplificado baseado em **microsserviços** para fins de avaliação técnica.

O sistema permite:

- cadastro de usuários
- autenticação
- movimentações financeiras (crédito e débito)
- transferência entre contas da mesma instituição
- consulta de saldo

Este repositório contém **duas APIs principais**:

- **ContaCorrente API**
- **Transferencia API**

---

# Visão de Arquitetura

O sistema foi projetado utilizando arquitetura baseada em **microsserviços**, onde cada serviço possui responsabilidade específica.

Fluxo simplificado:

Cliente → ContaCorrente API → Transferencia API → PostgreSQL

Responsabilidades:

## ContaCorrente API

- cadastro de usuário
- autenticação
- movimentação de conta (crédito e débito)
- consulta de saldo
- consulta de conta por CPF

## Transferencia API

- transferência entre contas da mesma instituição
- integração com a API ContaCorrente para realizar débito e crédito

---

# Tecnologias Utilizadas

- .NET 8
- ASP.NET Web API
- Dapper
- MediatR
- PostgreSQL
- Docker
- Docker Compose
- Swagger (OpenAPI)
- xUnit (testes automatizados)

---

# Estrutura do Repositório

conta-corrente/  
transferencia/  
banco-de-dados/  
docs/

Arquivos importantes:

docker-compose.yaml  
banco-de-dados/script-banco-de-dados.sql

---

# Documentação do Projeto

Durante o desenvolvimento foram criados dois documentos importantes para registrar o entendimento do desafio e as decisões arquiteturais tomadas.

## Entendimento do desafio

docs/entendimento_do_desafio.md

Este documento contém:

- interpretação detalhada do enunciado
- regras de negócio identificadas
- definição dos endpoints
- contratos das APIs
- validações aplicadas

## Documento de decisões

docs/documento_de_decisoes.md

Este documento registra:

- decisões arquiteturais tomadas
- justificativas técnicas
- adaptações feitas em relação ao enunciado
- modelagem final adotada

Esses documentos ajudam a explicar **por que determinadas decisões foram tomadas** durante a implementação.

---

# Banco de Dados

Banco utilizado:

PostgreSQL

Script de criação das tabelas:

banco-de-dados/script-banco-de-dados.sql

O script cria:

- tabela contacorrente
- tabela movimento
- tabela transferencia
- tabela idempotencia
- view de saldo

Esse script é executado automaticamente quando o container do banco é iniciado pela primeira vez.

---

# Como Executar o Projeto

## Pré-requisitos

- Docker
- Docker Compose

## Subir todo o ambiente

Na raiz do projeto execute:

docker compose up --build

Este comando irá iniciar:

- PostgreSQL
- ContaCorrente API
- Transferencia API

---

# Endpoints das APIs

Após subir o ambiente:

## ContaCorrente API

http://localhost:5215/swagger

Principais endpoints:

POST /usuario/cadastrar  
POST /auth/login  
POST /conta/movimentar  
GET /conta/saldo  
POST /conta/consultar

---

## Transferencia API

http://localhost:5216/swagger

Endpoint principal:

POST /transferencia/transferir

Exemplo de body:

{
  "id_requisicao": "uuid",
  "conta_destino": "uuid",
  "valor": 100.00
}

---

# Fluxo de Uso do Sistema

1. Criar usuário

POST /usuario/cadastrar

2. Fazer login

POST /auth/login

Retorna um token JWT.

3. Realizar movimentação

POST /conta/movimentar

Tipos:

C = crédito  
D = débito

4. Realizar transferência

POST /transferencia/transferir

5. Consultar saldo

GET /conta/saldo

---

# Autenticação

A autenticação é feita utilizando **JWT (JSON Web Token)**.

O token obtido no login deve ser enviado no header:

Authorization: Bearer {{token}}

---

# Idempotência

Operações críticas utilizam controle de **idempotência** através da tabela:

idempotencia

Isso evita execução duplicada de requisições em cenários de retry.

---

# Testes Automatizados

Os testes automatizados foram implementados utilizando:

xUnit

Cada API possui seu próprio projeto de testes.

---

# Observações

- APIs seguem padrão REST
- comunicação entre serviços via HTTP
- arquitetura preparada para execução em containers
- projeto preparado para execução em múltiplas réplicas


