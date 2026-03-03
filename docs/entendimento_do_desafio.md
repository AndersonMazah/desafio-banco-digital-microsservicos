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

#### POST **Cadastrar** ("/usuario/cadastrar"):  

obs.: Não recebe TOKEN JWT no HEADER (ver mais sobre isso em "documento_de_decisoes.md");

1. Recebe no body o "nome", "cpf" e "senha":  
    {  
        "nome": "Nome do usuário",  
        "cpf": "000.000.000-00",  
        "senha": "Senha do usuário"  
    }  

2. Validar campo "nome" no modelo de entrada: string obrigatória, remover espaços no início e fim, mínimo 1 e máximo 120 caracteres.  
    - caso seja inválido, retornar:
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "Nome inválido",  
        "type": "TYPE_INVALID_VALUE",  
        "data:" null  
    }  

3. Validar campo "cpf" no modelo de entrada: string obrigatória, remover espaços no início e fim, mínimo e máximo de 11 caracteres, todos numéricos.  
    - caso seja inválido, retornar:
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "CPF inválido",  
        "type": "TYPE_INVALID_DOCUMENT",  
        "data:" null  
    }  

4. Validar campo "senha" no modelo de entrada: string obrigatória, remover espaços no início e fim, mínimo e máximo de 6 caracteres.  
    - caso seja inválido, retornar status code STATUS_CODE_400_BAD_REQUEST com o seguinte response:  
    {  
        "message": "SENHA inválida",  
        "type": "TYPE_INVALID_VALUE",  
        "data:" null  
    }  

5. Validar o CPF conforme algoritmo de validação de CPF.  
    - caso seja inválido, retornar:   
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "CPF inválido",  
        "type": "TYPE_INVALID_DOCUMENT",  
        "data:" null  
    }  

6. Valida se o CPF já está cadastrado no banco.  
    - se já estiver cadastrado, retorna:  
    STATUS_CODE_409_CONFLICT  
    {  
        "message": "CPF já cadastrado",  
        "type": "TYPE_ALREADY_EXISTS",  
        "data:" null  
    }  

7. Caso os dados estejam válidos, então:  
    - Gerar um "salt" aleatório, em seguida gerar um "senha_hash" usando ("senha" + "salt").  
    - Inserir um registro no banco na tabela "contacorrente", com as seguintes informações:  
          insert into contacorrente (idcontacorrente, nome, cpf, senha_hash, salt) values ('new uuid()', 'nome do cliente', 'cpf', 'senha_hash', 'salt');  
    - Obter e retornar o número da conta do registro recém inserido:  
    STATUS_CODE_201_CREATED  
    {  
        "message": "Usuário cadastrado",  
        "type": "TYPE_SUCCESS",  
        "data":  {  
            "conta": "número da conta"  
        }  
    }  


#### PATCH **INATIVAR** ("/usuario/inativar"):  

1. Validar se o token JWT presente no header é válido.  
    - caso seja inválido, retornar:  
    STATUS_CODE_401_UNAUTHORIZED  
    {  
        "message": "Usuário não autorizado",  
        "type": "TYPE_USER_UNAUTHORIZED",  
        "data:" null  
    }  

2. Obter o ID da conta corrente que está dentro do token JWT;  

3. Recebe a senha no body:  
    {  
        "senha": "Senha do usuário"  
    }  

4. Validar o campo "senha" no modelo de entrada: string obrigatória, remover espaços no início e fim, mínimo e máximo de 6 caracteres.  
    - caso seja inválido, retornar:  
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "SENHA inválida",  
        "type": "TYPE_INVALID_VALUE",  
        "data:" null  
    }  

5. Buscar o registro da conta corrente que está no banco via o ID do mesmo.  
    - caso não encontre o registro, retornar.  
    STATUS_CODE_404_NOT_FOUND  
    {  
        "message": "Conta inválida",  
        "type": "TYPE_INVALID_ACCOUNT",  
        "data:" null  
    }  

6. Validar se o registro já está com status ativo.  
    - caso registro esteja com status inativo, retornar:  
    STATUS_CODE_409_CONFLICT  
    {  
        "message": "A conta já está inativa",  
        "type": "TYPE_INVALID_ACCOUNT",  
        "data:" null  
    }  

7. Validar se a senha informada confere com a senha do banco (via validação com senha_hash + salt).  
    - caso a senha não confira, retornar:  
    STATUS_CODE_401_UNAUTHORIZED  
    {  
        "message": "Usuário não autorizado",  
        "type": "TYPE_USER_UNAUTHORIZED",  
        "data:" null  
    }  

8. Alterar o status para "ativo" do registro e persistir no banco e retornar:  
    STATUS_CODE_204_NO_CONTENT  
    {  
        "message": "Usuário Inativado",  
        "type": "TYPE_SUCCESS",  
        "data:" null  
    }  


### Controller **AUTH**:

#### POST **Login** ("/auth/login"):  

obs.: Não recebe TOKEN JWT no HEADER (ver mais sobre isso em "documento_de_decisoes.md");  

1. Recebe ("conta" ou "cpf") e "senha":  
    {  
        "conta": "número da conta",  
        "cpf": "000.000.000-00",  
        "senha": "Senha do usuário"  
    }  

2. Validar o campo "conta" (opcional). Se houver valor neste campo, então este valor deve ser validado: Se é inteiro e se esta compreendido entre 1 e o valor máximo de um lont (long.MaxValue);  
    - caso tenha valor e o valor seja inválido, retornar:  
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "Conta inválida",  
        "type": "TYPE_INVALID_VALUE",  
        "data:" null  
    }  

3. Validar o campo "cpf" (opcional). Se houver valor, remover os espaços, remover (".","-") e o restante deve ter no mínimo e máximo de 11 caracteres e este cpf deve estar válido (validar conforme regra de validação de CPF);  
    - caso tenha valor e o valor seja inválido, retornar:  
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "CPF inválido",  
        "type": "TYPE_INVALID_VALUE",  
        "data:" null  
    }  

4. Validar se ao menos um dos campos está preenchido (e válido), ou o campo "conta" ou o campo "cpf";  
    - caso nenhum dos campos esteja preenchido, retornar:  
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "É necessário informar a Conta ou o CPF",  
        "type": "TYPE_INVALID_VALUE",  
        "data:" null  
    }  

5. Validar o campo "senha", que é obrigatório. Deve ser removido os espaços no início e fim, e ter no mínimo e no máximo 6 caracteres.  
    - caso seja inválido, retornar:  
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "SENHA inválida",  
        "type": "TYPE_INVALID_VALUE",  
        "data:" null  
    }  

6. Buscar o registro da conta corrente que está no banco via "conta" ou "cpf" (o que vier primeiro);  
    - caso não encontre o registro, retornar.  
    STATUS_CODE_404_NOT_FOUND  
    {  
        "message": "Usuário não autorizado",  
        "type": "TYPE_NOT_FOUND",  
        "data:" null  
    }  

7. Validar se a senha informada confere com a senha do banco (via validação com senha_hash + salt).  
    - caso a senha não confira, retornar:  
    STATUS_CODE_401_UNAUTHORIZED  
    {  
        "message": "Usuário não autorizado",  
        "type": "TYPE_USER_UNAUTHORIZED",  
        "data:" null  
    }  

8. Validar se o registro da conta corrente está com status ativo;  
    - caso não esteja, retornar:  
    STATUS_CODE_401_UNAUTHORIZED  
    {  
        "message": "Usuário não autorizado",  
        "type": "TYPE_USER_UNAUTHORIZED",  
        "data:" null  
    }  

9. Se não houver nenhum erro, então, gerar token JWT contendo (id do usuário) e retornar:  
    STATUS_CODE_200_OK  
    {  
        "message": "Usuário autenticado",  
        "type": "TYPE_USER_AUTHORIZED",  
        "data:" token_jwt  
    }  


### Controller **CONTA**:

#### POST **movimentar** ("/conta/movimentar"):  

1. Validar se o token JWT presente no header é válido.  
    - caso seja inválido, retornar:  
    STATUS_CODE_401_UNAUTHORIZED  
    {  
        "message": "Usuário não autorizado",  
        "type": "TYPE_USER_UNAUTHORIZED",  
        "data:" null  
    }  

2. Obter o ID da conta corrente que está dentro do token JWT;  

3. Recebe no body: "conta" (opcional), "id_requisicao", "valor" e "tipo":  
    {  
        "conta": "número nullável (opcional)",  
        "id_requisicao": "uuid vindo do front",  
        "valor": 0.01,  
        "tipo": "[C|D]"  
    }  

4. Validar o campo "conta" (opcional). Se houver valor neste campo, então este valor deve ser validado: Se é inteiro e se esta compreendido entre 1 e o valor máximo de um lont (long.MaxValue);  
    - caso tenha valor e o valor seja inválido, retornar:
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "Conta inválida",  
        "type": "TYPE_INVALID_VALUE",  
        "data:" null  
    }  

5. id_requisicao": TENHO DÚVIDAS DE COMO PROCEDER COM ESTA INFORMAÇÃO: TODO : VALIDAR, VERIFICAR?  
    - caso seja inválido, retornar:
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "ID inválida",  
        "type": "TYPE_INVALID_VALUE",  
        "data:" null  
    }  

6. Validar o campo "valor" (obrigatório). Deve ser um valor maior que zero, com duas casas decimais;  
    - caso seja inválido, retornar:
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "Valor inválido",  
        "type": "TYPE_INVALID_VALUE",  
        "data:" null  
    }  

7. Validar o campo "tipo" (obrigatório). Deve conter a letra "C" ou a letra "D";  
    - caso seja inválido, retornar:
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "Tipo inválido",  
        "type": "TYPE_INVALID_VALUE",  
        "data:" null  
    }  

8. Caso a conta que vier no body tenha valor, considerar ela, se não, considerar o id da conta corrente presente no token, ou seja:
    - será considerado a "conta" se ela estiver presente no body, ou, será considerado o uuid da conta presente no token.

9. Buscar o registro da conta corrente que está no banco via "conta" ou "uuid do token";  
    - caso não encontre o registro, retornar.  
    STATUS_CODE_404_NOT_FOUND  
    {  
        "message": "Conta não localizada",  
        "type": "TYPE_INVALID_ACCOUNT",  
        "data:" null  
    }  

10. Validar se o registro da conta corrente está ativo;  
    - caso não esteja ativo, retornar.  
    STATUS_CODE_409_CONFLICT  
    {  
        "message": "Conta está inativa",  
        "type": "TYPE_INACTIVE_ACCOUNT",  
        "data:" null  
    }  

11. Se o id do registro de conta corrente não pertencer ao usuário logado, validar se o tipo é "C";
    - caso não seja, retornar.  
    STATUS_CODE_409_CONFLICT  
    {  
        "message": "Movimento de conta não permitido",  
        "type": "TYPE_INVALID_TYPE",  
        "data:" null  
    }  

12. Caso não tenha nenhum erro, então, inserir um registro na tabela movimento contendo ("idcontacorrente", "datamovimento", "tipo" e "valor") e retornar:  
    STATUS_CODE_204_NO_CONTENT
    {  
        "message": "Movimento cadastrado com sucesso",  
        "type": "TYPE_SUCCESS",  
        "data:" null  
    }  











---

## Padrão de status code:  

- STATUS_CODE_200_OK - Requisição bem sucedida.  
- STATUS_CODE_201_CREATED - Quando um recurso foi criado com sucesso.  
- STATUS_CODE_204_NO_CONTENT - Quando a operação foi concluído e não tem conteúdo de retorno.  

- STATUS_CODE_400_BAD_REQUEST - Quando a requisição é inválida.  
- STATUS_CODE_401_UNAUTHORIZED - Quando não há token ou ele está inválido.  
- STATUS_CODE_403_FORBIDDEN - Quando o usuário não tem permissão.  
- STATUS_CODE_404_NOT_FOUND - Quando o recurso não existe.
- STATUS_CODE_409_CONFLICT - Quando o recurso já existe ou, conflito de informações.  


## Padrão de respostas rest:
    {  
        "message": "frase pequena, no máximo uns 50 caracteres",  
        "type": type,  
        "data":  null  
    }  

### TYPE:  

#### API Conta Corrente:  

##### Cadastro:  

- TYPE_INVALID_DOCUMENT- Quando o CPF for inválido.  
- TYPE_ALREADY_EXISTS (minha sugestão) - CPF já está cadastrado no sistema;  

##### Login:  
- TYPE_USER_UNAUTHORIZED - Quando número/CPF ou senha estiver incorreto.  
- TYPE_NOT_FOUND (minha sugestão) - Registro não encontrado;  

##### Inativar:  
- TYPE_INVALID_ACCOUNT - Apenas contas cadastradas podem receber movimentação.  
- TYPE_SUCCESS (minha sugestão)- Operação realizada com sucesso.  

##### Movimentacao:  

- TYPE_INVALID_ACCOUNT - Apenas contas cadastradas podem receber movimentação.  
- TYPE_INACTIVE_ACCOUNT - Apenas contas ativas podem receber movimentação.  
- TYPE_INVALID_VALUE - Apenas valores positivos podem ser recebidos.  
- TYPE_INVALID_TYPE - Tipo diferente de débito ou crédito.  
- TYPE_INVALID_TYPE = Tipo débito não permitido para conta diferente do usuário logado.  


- TYPE_USER_AUTHORIZED;

##### Saldo:  

- TYPE_INVALID_ACCOUNT - Conta não cadastrada.  
- TYPE_INACTIVE_ACCOUNT - Apenas contas ativas podem receber movimentação.  

#### API Transferência:  

##### Transferir:  

- TYPE_INVALID_ACCOUNT - Apenas contas cadastradas podem realizar transferência.  
- TYPE_INACTIVE_ACCOUNT - Apenas contas ativas podem realizar transferência.  
- TYPE_INVALID_VALUE - Valor não positivo.  

---




