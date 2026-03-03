# Este documento contém minhas anotações referentes a como interpretei a leitura do desafio.

--- 

## Requisitos Funcionais:  
- Cadastro e autenticação de usuário;
- Realização de movimentos na conta-corrente (depósito e saque);
- Transferência entre contas da mesma instituição;
- Consulta de saldo;

## Requisitos Não Funcionais:
- Arquitetura baseada em microsserviços;
- Cada microsserviço deve aplicar DDD;
- Cada microsserviço deve aplicar CQRS;
- Todos endpoints devem ser protegidos com token JWT;
- Dados como, CPF, Número da conta, só podem transitar no microsserviço do usuário;
- Todas as APIs devem conter projeto de testes automatizados;
- Cada API deve rodar dentro de um contêiner docker;
- Cada API deve estar preparada para ser executada em múltiplas réplicas (Kubernetes);
- Os endpoints devem ser idempotentes (exceto Cadastro e Login);

## Restrições Técnicas:
- Cada API deve ser desenvolvida em .NET 8;
- O banco de dados deve ser o SQLite;

---

## Banco de dados:

CREATE TABLE contacorrente (  
  idcontacorrente UUID PRIMARY KEY,  
  numero BIGINT GENERATED ALWAYS AS IDENTITY UNIQUE,  
  cpf VARCHAR(11) NOT NULL UNIQUE,  
  nome VARCHAR(120) NOT NULL,  
  ativo BOOLEAN NOT NULL DEFAULT TRUE,  
  senha_hash BYTEA NOT NULL,  
  salt BYTEA NOT NULL  
);  

---

# API Conta-Corrente:  

### Controller **USUARIO**:  

#### POST **Cadastrar** ("/conta-corrente/usuario/cadastrar"):  

- Recebe o Nome, CPF e Senha:  
    {  
        "nome": "Nome do usuário",  
        "cpf": "000.000.000-00",  
        "senha": "Senha do usuário"  
    }  

- Valida os campos Nome, CPF e Senha:  
    - Nome: string, não nullo, mínimo 1 caracter e máximo 120 caracteres;  
    - CPF: string, não nullo, mínimo e máximo de 11 caracteres;  
    - Senha: string, não nullo, mínimo e máximo de 6 caracteres;  
    {  
        "message": "CPF Inválido",  
        "type": "INVALID_DOCUMENT",  
        "data:" null  
    }  

- Valida o CPF, e caso seja inválido, então, retornar status code 400:  
    {  
        "message": "CPF Inválido",  
        "type": "INVALID_DOCUMENT",  
        "data:" null  
    }  

- Valida se o CPF já está cadastrado no banco, se já estiver cadastrado, então retorna status code 409:  
    {  
        "message": "CPF já cadastrado",  
        "type": "DOCUMENT_ALREADY_REGISTERED",  
        "data:" null  
    }  

- Caso os dados estejam válidos, então:    
    - Gerar um salt aleatório, em seguida gerar um senha_hash usando (senha + salt).  
    - Inserir um registro no banco na tabela "contacorrente", com as seguintes informações:  
          insert into contacorrente (idcontacorrente, nome, cpf, senha_hash, salt) values ('abc..uuid', 'nome do cliente', 'cpf', 'senha_hash', 'salt');  
    - Obter o número da conta do registro recém inserido e retornar o status code 201 juntamente com este numero da conta.  
    {  
        "message": "Usuário cadastrado",  
        "type": "CUSTOMER_CREATED",  
        "data":  {  
            "conta": "número da conta"  
        }  
    }  


### Controller **AUTH**:

#### POST **Login** ("/conta-corrente/auth/login"):  
- Não recebe TOKEN JWT no HEADER;

- Recebe (Numero da Conta ou CPF) e Senha:  
    {  
        "conta": "número da conta",  
        "cpf": "000.000.000-00",  
        "senha": "Senha do usuário"  
    }  

- Validar os campos:  
    - Conta: inteiro nulável. Se houver valor, validar se é inteiro e se esta compreendido entre 1 e o valor máximo de um lont (long.MaxValue);  
    - CPF: string nulável. Se houver valor, remover os espaços, remover (".","-") e o restante deve ter no mínimo e máximo de 11 caracteres, e este cpf deve estar válido (validar conforme regra de validação de CPF);  
    - Senha: string, não nullo, mínimo e máximo de 6 caracteres;  
    - Validar se existe conta, se existir, considerar o login com conta, senão existir, validar se existe cpf e, se existir, considerar login com cpf;  
    - Se nem numero da conta e nem cpf existirem, então, retornar status code 401:  
    {  
        "message": "Usuário não autorizado",  
        "type": "USER_UNAUTHORIZED",  
        "data:" null  
    }  

- Localizar o registro do usuário pela conta ou CPF, caso não encontre, então, retornar status code 401:  
    {  
        "message": "Usuário não autorizado",  
        "type": "USER_UNAUTHORIZED",  
        "data:" null  
    }  

- Validar a senha com a senha_hash e salt no banco., caso o hash resultante não confira, então, retornar status code 401:  
    {  
        "message": "Usuário não autorizado",  
        "type": "USER_UNAUTHORIZED",  
        "data:" null  
    }  

- Validar se o usuário está ativo, caso não esteja, retornar status code 401:  
    {  
        "message": "Usuário não autorizado",  
        "type": "USER_UNAUTHORIZED",  
        "data:" null  
    }  

- Caso o usuário seja localizado (pela conta ou cpf) e a senha esteja correta, então:  
    - Gerar token JWT contendo (id do usuário) e retornar no body com status 200;
    {  
        "message": "Usuário autenticado",  
        "type": "USER_AUTHORIZED",  
        "data:" token_jwt  
    }  


#### PATCH **Cadastrar** ("/conta-corrente/usuario/inativar"):  
- Recebe TOKEN JWT no HEADER. Validar se o token é válido, senão for, então retorna status code 403;  

- Obter o ID da conta corrente que está dentro do token;  

- Recebe a senha no body:  
    {  
        "senha": "Senha do usuário"  
    }  

- Buscar o registro do usuário que está no banco via o ID do mesmo. Senão encontrar ou se encontrar e este já estiver inativo, retornar status code 401.  

- Validar se a senha é válida, senaõ for, retornar status code 403.  

- Validar o status "ativo" do registro do usuário para false e retornar status code 204.  





















---

## Padrão de respostas rest:
    {  
        "message": "Usuário cadastrado",  
        "type": "SUCESSO",  
        "data":  null
    }

   type:  
      - INVALID_DOCUMENT;  
      - DOCUMENT_ALREADY_REGISTERED;  
      - CUSTOMER_CREATED;  
      - USER_UNAUTHORIZED;
      - USER_AUTHORIZED;

---




