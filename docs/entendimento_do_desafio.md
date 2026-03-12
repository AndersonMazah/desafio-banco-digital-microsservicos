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
- O banco de dados é o PostgreSQL (ver documento de decisões);  

---

## Banco de dados:  

CREATE DATABASE "desafio-banco-digital";  

CREATE TABLE contacorrente (  
  idcontacorrente UUID PRIMARY KEY DEFAULT gen_random_uuid(),  
  numero BIGINT GENERATED ALWAYS AS IDENTITY UNIQUE,  
  cpf CHAR(11) NOT NULL UNIQUE CHECK (cpf ~ '^[0-9]{11}$'),  
  nome VARCHAR(120) NOT NULL,  
  ativo BOOLEAN NOT NULL DEFAULT TRUE,  
  senha_hash BYTEA NOT NULL,  
  salt BYTEA NOT NULL  
);  

CREATE TABLE movimento (  
  idmovimento UUID PRIMARY KEY DEFAULT gen_random_uuid(),  
  idcontacorrente UUID NOT NULL,  
  datamvto TIMESTAMPTZ NOT NULL DEFAULT now(),  
  tipo CHAR(1) NOT NULL CHECK (tipo IN ('C','D')),  
  valor NUMERIC(16,2) NOT NULL CHECK (valor > 0)  
);  
ALTER TABLE movimento ADD CONSTRAINT movimento_contacorrente_fk FOREIGN KEY (idcontacorrente) REFERENCES contacorrente (idcontacorrente);  
CREATE INDEX movimento_idcontacorrente_idx ON movimento (idcontacorrente);  

CREATE VIEW vw_saldo_conta AS  
SELECT  
  c.idcontacorrente,  
  COALESCE(  
    SUM(m.valor) FILTER (WHERE m.tipo = 'C'),  
    0  
  ) - COALESCE(  
    SUM(m.valor) FILTER (WHERE m.tipo = 'D'),  
    0  
  ) AS saldo  
FROM contacorrente c  
LEFT JOIN movimento m ON m.idcontacorrente = c.idcontacorrente  
GROUP BY c.idcontacorrente;  

CREATE TABLE idempotencia (  
  ididempotencia UUID PRIMARY KEY DEFAULT gen_random_uuid(),  
  requisicao UUID NOT NULL UNIQUE,         /* aqui será mitigado possíveis problemas de concorrência */  
  status BOOLEAN NOT NULL DEFAULT false,   /* false=Processo em andamento, true=Processo já concluído */  
  status_code CHAR(3) NOT NULL DEFAULT '202',  
  resultado TEXT  
);  

CREATE TABLE transferencia (  
  idtransferencia UUID PRIMARY KEY DEFAULT gen_random_uuid(),  
  idconta_origem UUID NOT NULL,  
  idconta_destino UUID NOT NULL,  
  datamvto TIMESTAMPTZ NOT NULL DEFAULT now(),  
  valor NUMERIC(16,2) NOT NULL CHECK (valor > 0)  
);  
ALTER TABLE transferencia ADD CONSTRAINT chk_contas_diferentes CHECK (idconta_origem <> idconta_destino);  
ALTER TABLE transferencia ADD CONSTRAINT fk_conta_origem  FOREIGN KEY (idconta_origem)  REFERENCES contacorrente(idcontacorrente);  
ALTER TABLE transferencia ADD CONSTRAINT fk_conta_destino FOREIGN KEY (idconta_destino) REFERENCES contacorrente(idcontacorrente);  



## Premissas assumidas (anotadas no documento de decisões.md)
- é permitido movimentar conta com saldo negativo;  
- a senha foi fixada em 6 caracteres alfanuméricos;  
- o CPF é armazenado sem máscara;  
- o token JWT contém idcontacorrente do usuário;  

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
        "data": null  
    }  

3. Validar campo "cpf" no modelo de entrada: string obrigatória, remover máscara (".", "-"), espaços no início e fim, mínimo e máximo de 11 caracteres, todos numéricos.  
    - caso seja inválido, retornar:  
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "CPF inválido",  
        "type": "TYPE_INVALID_DOCUMENT",  
        "data": null  
    }  

4. Validar campo "senha" no modelo de entrada: string obrigatória, remover espaços no início e fim, mínimo e máximo de 6 caracteres.  
    - caso seja inválido, retornar:  
    STATUS_CODE_400_BAD_REQUEST
    {  
        "message": "SENHA inválida",  
        "type": "TYPE_INVALID_VALUE",  
        "data": null  
    }  

5. Validar o CPF conforme algoritmo de validação de CPF.  
    - caso seja inválido, retornar:   
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "CPF inválido",  
        "type": "TYPE_INVALID_DOCUMENT",  
        "data": null  
    }  

6. Valida se o CPF já está cadastrado no banco.  
    - se já estiver cadastrado, retorna:  
    STATUS_CODE_409_CONFLICT  
    {  
        "message": "CPF já cadastrado",  
        "type": "TYPE_ALREADY_EXISTS",  
        "data": null  
    }  

7. Caso os dados estejam válidos, então:  
    - Gerar um "salt" aleatório, em seguida gerar um "senha_hash" usando ("senha" + "salt").  
    - Inserir um registro no banco na tabela "contacorrente", com as seguintes informações:  
          INSERT INTO contacorrente (idcontacorrente, nome, cpf, senha_hash, salt) values ('new uuid()', 'nome do cliente', 'cpf', 'senha_hash', 'salt');  
          RETURNING numero;
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
        "data": null  
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
        "data": null  
    }  

5. Buscar o registro da conta corrente que está no banco via o ID do mesmo.  
    - caso não encontre o registro, retornar.  
    STATUS_CODE_404_NOT_FOUND  
    {  
        "message": "Conta inválida",  
        "type": "TYPE_INVALID_ACCOUNT",  
        "data": null  
    }  

6. Validar se o registro já está com status ativo.  
    - caso registro esteja com status inativo, retornar:  
    STATUS_CODE_409_CONFLICT  
    {  
        "message": "A conta já está inativa",  
        "type": "TYPE_INVALID_ACCOUNT",  
        "data": null  
    }  

7. Validar se a senha informada confere com a senha do banco (via validação com senha_hash + salt).  
    - caso a senha não confira, retornar:  
    STATUS_CODE_401_UNAUTHORIZED  
    {  
        "message": "Usuário não autorizado",  
        "type": "TYPE_USER_UNAUTHORIZED",  
        "data": null  
    }  

8. Alterar o campo "ativo" para false, persistir no banco e retornar:  
    STATUS_CODE_204_NO_CONTENT  


### Controller **AUTH**:

#### POST **Login** ("/auth/login"):  

obs.: Não recebe TOKEN JWT no HEADER (ver mais sobre isso em "documento_de_decisoes.md");  

1. Recebe ("conta" ou "cpf") e "senha":  
    {  
        "conta": "número da conta",  
        "cpf": "000.000.000-00",  
        "senha": "Senha do usuário"  
    }  

2. Validar o campo "conta" (opcional). Se houver valor neste campo, então este valor deve ser validado: Se é inteiro e se esta compreendido entre 1 e o valor máximo de um long (long.MaxValue);  
    - caso tenha valor e o valor seja inválido, retornar:  
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "Conta inválida",  
        "type": "TYPE_INVALID_VALUE",  
        "data": null  
    }  

3. Validar campo "cpf" (opcional). no modelo de entrada: string obrigatória, remover máscara (".", "-"), espaços no início e fim, mínimo e máximo de 11 caracteres, todos numéricos.  
    - caso tenha valor e o valor seja inválido, retornar:  
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "CPF inválido",  
        "type": "TYPE_INVALID_VALUE",  
        "data": null  
    }  

4. Validar se ao menos um dos campos está preenchido (e válido), ou o campo "conta" ou o campo "cpf";  
    - caso nenhum dos campos esteja preenchido, retornar:  
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "É necessário informar a Conta ou o CPF",  
        "type": "TYPE_INVALID_VALUE",  
        "data": null  
    }  

5. Validar o campo "senha", que é obrigatório. Deve ser removido os espaços no início e fim, e ter no mínimo e no máximo 6 caracteres.  
    - caso seja inválido, retornar:  
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "SENHA inválida",  
        "type": "TYPE_INVALID_VALUE",  
        "data": null  
    }  

6. Buscar o registro da conta corrente que está no banco via "conta" ou "cpf" (se conta estiver preenchida, usar conta, senão, usar cpf);  
    - caso não encontre o registro, retornar.  
    STATUS_CODE_404_NOT_FOUND  
    {  
        "message": "Usuário não autorizado",  
        "type": "TYPE_USER_UNAUTHORIZED",  
        "data": null  
    }  

7. Validar se a senha informada confere com a senha do banco (via validação com senha_hash + salt).  
    - caso a senha não confira, retornar:  
    STATUS_CODE_401_UNAUTHORIZED  
    {  
        "message": "Usuário não autorizado",  
        "type": "TYPE_USER_UNAUTHORIZED",  
        "data": null  
    }  

8. Validar se o registro da conta corrente está com status ativo;  
    - caso não esteja, retornar:  
    STATUS_CODE_401_UNAUTHORIZED  
    {  
        "message": "Usuário não autorizado",  
        "type": "TYPE_USER_UNAUTHORIZED",  
        "data": null  
    }  

9. Se não houver nenhum erro, então, gerar token JWT contendo (id do usuário) e retornar:  
    STATUS_CODE_200_OK  
    {  
        "message": "Usuário autenticado",  
        "type": "TYPE_USER_AUTHORIZED",  
        "data": token_jwt  
    }  


### Controller **CONTA**:

#### POST **movimentar** ("/conta/movimentar"):  

1. Validar se o token JWT presente no header é válido.  
    - caso seja inválido, retornar:  
    STATUS_CODE_401_UNAUTHORIZED  
    {  
        "message": "Usuário não autorizado",  
        "type": "TYPE_USER_UNAUTHORIZED",  
        "data": null  
    }  

2. Obter o ID da conta corrente do usuário (que está dentro do token JWT);  

3. Recebe no body: "uuid da conta corrente", "id_requisicao", "valor" e "tipo":  
    {  
        "conta": "uuid da conta corrente",  
        "id_requisicao": "uuid vindo do front",  
        "valor": 0.01,  
        "tipo": "[C|D]"  
    }  

4. Validar o campo "conta". Deve ser do tipo **uuid** e obrigatório;  
    - caso seja inválido, retornar:  
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "Conta inválida",  
        "type": "TYPE_INVALID_VALUE",  
        "data": null  
    }  

5. Validar o campo "id_requisicao". Deve ser do tipo **uuid** e obrigatório;  
    - caso seja inválido, retornar:  
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "ID Requisição inválido",  
        "type": "TYPE_INVALID_VALUE",  
        "data": null  
    }  

6. Validar o campo "valor" (obrigatório). Deve ser um valor maior que zero;  
    - caso seja inválido, retornar:  
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "Valor inválido",  
        "type": "TYPE_INVALID_VALUE",  
        "data": null  
    }  

7. Validar o campo "tipo" (obrigatório). Deve conter a letra "C" ou a letra "D";  
    - caso seja inválido, retornar:  
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "Tipo inválido",  
        "type": "TYPE_INVALID_VALUE",  
        "data": null  
    }  

8. Buscar na tabela "idempotencia", pelo campo da tabela "requisicao" o valor recebido em "id_idempotencia";  
   - Se encontrar registro:
      - Se o valor de "status"==false, (significa que o processo está em andamento), então retornar: 
      - Se o valor de "status"==true, (significa que o processo já terminou), então, retornar o valor que está no campo "resultado" juntamente o com o campo "status_code";  
   - Senão encontrar registro, então, inserir novo registro nesta tabela (requisicao=id_requisicao);  

9. Buscar o registro da conta corrente que está no banco (pelo uuid da conta corrente vindo no body);  
    - caso não encontre o registro, retornar.  
    STATUS_CODE_404_NOT_FOUND  
    - e atualizar o registro da tabela idempotencia (cujo "requisicao"="id_requisicao"") para: "status"="true" e preencher "status_code" e "resultado" com os valores retornados por esta requisicao;  
    {  
        "message": "Conta não localizada",  
        "type": "TYPE_INVALID_ACCOUNT",  
        "data": null  
    }  

10. Validar se o registro da conta corrente está ativo;  
    - caso não esteja ativo, retornar.  
    STATUS_CODE_409_CONFLICT  
    - e atualizar o registro da tabela idempotencia (cujo "requisicao"="id_requisicao"") para: "status"="true" e preencher "status_code" e "resultado" com os valores retornados por esta requisicao;  
    {  
        "message": "Conta está inativa",  
        "type": "TYPE_INACTIVE_ACCOUNT",  
        "data": null  
    }  

11. Verificar se o uuid da conta corrente vindo no body da requisão é igual ao uuid da conta corrente vindo no TOKEN JWT da requisição e se o "tipo" é "C". Se os uuid forem iguais e se o "tipo" for "C", então:  
    STATUS_CODE_400_BAD_REQUEST  
    - e atualizar o registro da tabela idempotencia (cujo "requisicao"="id_requisicao"") para: "status"="true" e preencher "status_code" e "resultado" com os valores retornados por esta requisicao;  
    {  
        "message": "Movimento de conta não permitido",  
        "type": "TYPE_INVALID_TYPE",  
        "data": null  
    }  

13. Caso não tenha nenhum erro, então;  
    - Inserir um registro na tabela movimento contendo ("idcontacorrente", "datamovimento", "tipo" e "valor");  
    - Atualizar o registro da tabela idempotencia (cujo "requisicao"="id_requisicao"") para: "status"="true" e preencher "status_code" e "resultado" com os valores retornados por esta requisicao;  
    STATUS_CODE_204_NO_CONTENT  



#### GET **saldo** ("/conta/saldo"):  

1. Validar se o token JWT presente no header é válido.  
    - caso seja inválido, retornar:  
    STATUS_CODE_401_UNAUTHORIZED  
    {  
        "message": "Usuário não autorizado",  
        "type": "TYPE_USER_UNAUTHORIZED",  
        "data": null  
    }  

2. Obter o ID da conta corrente que está dentro do token JWT;  

3. Buscar o registro da conta corrente que está no banco;  
    - caso não encontre o registro, retornar.  
    STATUS_CODE_404_NOT_FOUND  
    {  
        "message": "Conta não localizada",  
        "type": "TYPE_INVALID_ACCOUNT",  
        "data": null  
    }  

4. Validar se o registro da conta corrente está ativo;  
    - caso não esteja ativo, retornar.  
    STATUS_CODE_409_CONFLICT  
    {  
        "message": "Conta está inativa",  
        "type": "TYPE_INACTIVE_ACCOUNT",  
        "data": null  
    }  

5. Consultar a view "vw_saldo_conta" filtrando a conta e retornar:
    STATUS_CODE_200_OK  
    {  
        "message": "Consulta de saldo",  
        "type": "TYPE_SUCCESS",  
        "data": {
              "conta": "nr da conta corrente",
              "nome": "nome do titular da conta corrente",
              "data_hora": "data e hora da resposta da consulta",
              "saldo": "valor do saldo da conta",
        }  
    }  


#### POST **consultar** ("/conta/consultar"):
obs.: Este endpoint não estava previsto no enunciado e a decisão da inclusão dele pode ser vista no documento de decisão.md  

1. Validar se o token JWT presente no header é válido.  
    - caso seja inválido, retornar:  
    STATUS_CODE_401_UNAUTHORIZED  
    {  
        "message": "Usuário não autorizado",  
        "type": "TYPE_USER_UNAUTHORIZED",  
        "data": null  
    }  

2. Recebe no body: "cpf":  
    {  
        "cpf": "12345678901"   
    }  

3. Validar campo "cpf" que veio no corpo do método: string obrigatória, remover máscara (".", "-"), espaços no início e fim, mínimo e máximo de 11 caracteres, todos numéricos.  
    - caso seja inválido, retornar:  
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "CPF inválido",  
        "type": "TYPE_INVALID_VALUE",  
        "data": null  
    }  

4. Buscar o registro da conta corrente que está no banco, pesquisando pelo CPF;  
    - caso não encontre o registro, retornar.  
    STATUS_CODE_404_NOT_FOUND  
    {  
        "message": "Conta não localizada",  
        "type": "TYPE_INVALID_ACCOUNT",  
        "data": null  
    }  

5. Validar se o registro da conta corrente está ativo;  
    - caso não esteja ativo, retornar.  
    STATUS_CODE_404_NOT_FOUND  
    {  
        "message": "Conta não localizada",  
        "type": "TYPE_INVALID_ACCOUNT",  
        "data": null  
    }  

6. Retornar:
    obs.: Deve-se retornar apenas o primeiro nome do titulas da conta.
    STATUS_CODE_200_OK  
    {  
        "message": "Consulta de Cliente",  
        "type": "TYPE_SUCCESS",  
        "data": {  
              "conta": "nr da conta corrente",  
              "uuid": "uuid da conta corrente",  
              "nome": "retonar apenas o PRIMEIRO nome do titular da conta corrente",  
              "data_hora": "data e hora da resposta da consulta"  
        }  
    }  

# API Transferência:  

### Controller **transferencia**:  

#### POST **Transferir** ("/transferencia/transferir"):  

1. Validar se o token JWT presente no header é válido.  
    - caso seja inválido, retornar:  
    STATUS_CODE_401_UNAUTHORIZED  
    {  
        "message": "Usuário não autorizado",  
        "type": "TYPE_USER_UNAUTHORIZED",  
        "data": null  
    }  

2. Obter o ID da conta corrente (de origem) que está dentro do token JWT;  

3. Recebe no body: "conta_destino", "id_requisicao", "valor" e "tipo":  
    {  
        "id_requisicao": "uuid vindo do front",  
        "conta_destino": "uuid da conta de destino",  
        "valor": 0.01  
    }  

4. Validar o campo "id_requisicao". Deve ser do tipo **uuid** e obrigatório;  
    - caso seja inválido, retornar:  
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "ID Requisição inválido",  
        "type": "TYPE_INVALID_VALUE",  
        "data": null  
    }  

5. Validar o campo "conta_destino". Deve ser do tipo **uuid** e obrigatório;  
    - caso seja inválido, retornar:  
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "Conta destino inválida",  
        "type": "TYPE_INVALID_VALUE",  
        "data": null  
    }  

6. Validar o campo "valor" (obrigatório). Deve ser um valor maior que zero;  
    - caso seja inválido, retornar:  
    STATUS_CODE_400_BAD_REQUEST  
    {  
        "message": "Valor inválido",  
        "type": "TYPE_INVALID_VALUE",  
        "data": null  
    }  

7. Validar se o uuid da conta de destino é o mesmo uuid presente no token JWT e caso seja o mesmo, deve-se retornar;  
    STATUS_CODE_409_CONFLICT
    {  
        "message": "Não é possível transferir para a mesma conta",  
        "type": "TYPE_OPERATION_NOT_ALLOWED",  
        "data": null  
    }  

8. Buscar na tabela "idempotencia", pelo campo da tabela "requisicao" o valor recebido em "id_idempotencia";  
   - Se encontrar registro:
      - Se o valor de "status"==false, (significa que o processo está em andamento), então retornar: 
      - Se o valor de "status"==true, (significa que o processo já terminou), então, retornar o valor que está no campo "resultado" juntamente o com o campo "status_code";  
   - Senão encontrar registro, então, inserir novo registro nesta tabela (requisicao=id_requisicao);  

9. Buscar no banco o registro da conta corrente de origem (a que veio no TOKEN JWT);  
    - caso não encontre o registro, retornar.  
    STATUS_CODE_404_NOT_FOUND  
    - e atualizar o registro da tabela idempotencia (cujo "requisicao"="id_requisicao"") para: "status"="true" e preencher "status_code" e "resultado" com os valores retornados por esta requisicao;  
    {  
        "message": "Conta Corrente de Origem não localizada",  
        "type": "TYPE_INVALID_ACCOUNT",  
        "data": null  
    }  

10. Validar se o registro da conta corrente de origem está ativo;  
    - caso não esteja ativo, retornar.  
    STATUS_CODE_409_CONFLICT  
    - e atualizar o registro da tabela idempotencia (cujo "requisicao"="id_requisicao"") para: "status"="true" e preencher "status_code" e "resultado" com os valores retornados por esta requisicao;  
    {  
        "message": "Conta Corrente de Origem está inativa",  
        "type": "TYPE_INACTIVE_ACCOUNT",  
        "data": null  
    }  

11. Buscar no banco o registro da conta corrente de destino (a que veio no body da requisição);  
    - caso não encontre o registro, retornar.  
    STATUS_CODE_404_NOT_FOUND  
    - e atualizar o registro da tabela idempotencia (cujo "requisicao"="id_requisicao"") para: "status"="true" e preencher "status_code" e "resultado" com os valores retornados por esta requisicao;  
    {  
        "message": "Conta Corrente de Destino não localizada",  
        "type": "TYPE_INVALID_ACCOUNT",  
        "data": null  
    }  

12. Validar se o registro da conta corrente de destino está ativo;  
    - caso não esteja ativo, retornar.  
    STATUS_CODE_409_CONFLICT  
    - e atualizar o registro da tabela idempotencia (cujo "requisicao"="id_requisicao"") para: "status"="true" e preencher "status_code" e "resultado" com os valores retornados por esta requisicao;  
    {  
        "message": "Conta Corrente de Destino está inativa",  
        "type": "TYPE_INACTIVE_ACCOUNT",  
        "data": null  
    }  

13. Efetuar a chamada a api de conta-corrente/movimento, e efetuar um débito;  
    - enviar o TOKEN JWT e o body da requisição será:  
    {  
        "conta": "uuid da conta corrente de origem",  
        "id_requisicao": "gerar um novo uuid aleatório",  
        "valor": 0.01,  
        "tipo": "D"  
    }  
    obs.: 
        - a conta de origem já existe e foi validada, então não é para dar erro;  
        - o id_requisicao será gerado aleatóriamente, e não é para dar erro;  
        - o valor já foi validado (que é diferente de zero e positivo);  
        - o tipo é "D", e também não é para dar erro;  
        - o uuid presente no TOKEN JWT é o mesmo do enviado via body, porém, o tipo será "D", então isso não dará erro;  
        - ou seja, não foi possível identificar nenhuma falha lógica nesta operação;  
    - Após chamar esta api, deve-se aguardar o retorno. Com o retorno, validar se o mesmo é STATUS_CODE_204_NO_CONTENT.
        - Se o status code de retorno da chamada a outra api for STATUS_CODE_204_NO_CONTENT, então prossegue para o próximo passo;
        - Se o status code de retorno da chamada a outra api não for STATUS_CODE_204_NO_CONTENT, então, deve-se atualizar o registro da tabela idempotencia (cujo "requisicao"="id_requisicao"") para: "status"="true" e preencher "status_code" e "resultado" com os valores retornados por esta requisicao;  

14. Efetuar a chamada a api de conta-corrente/movimento, e efetuar um crédito;  
    - enviar o TOKEN JWT e o body da requisição será:  
    {  
        "conta": "uuid da conta corrente de destino",  
        "id_requisicao": "gerar um novo uuid aleatório",  
        "valor": 0.01,  
        "tipo": "C"  
    }  
    obs.: 
        - a conta de destino já existe e foi validada, então não é para dar erro;  
        - o id_requisicao será gerado aleatóriamente, e não é para dar erro;  
        - o valor já foi validado (que é diferente de zero e positivo);  
        - o tipo é "C", e também não é para dar erro;  
        - o uuid presente no TOKEN JWT não é o mesmo do enviado via body, então isso não dará erro;  
        - ou seja, não foi possível identificar nenhuma falha lógica nesta operação;  
    - Após chamar esta api, deve-se aguardar o retorno. Com o retorno, validar se o mesmo é STATUS_CODE_204_NO_CONTENT.
        - Se o status code de retorno da chamada a outra api for STATUS_CODE_204_NO_CONTENT, então prossegue para o próximo passo;
        - Se o status code de retorno da chamada a outra api não for STATUS_CODE_204_NO_CONTENT, então, deve-se atualizar o registro da tabela idempotencia (cujo "requisicao"="id_requisicao"") para: "status"="true" e preencher "status_code" e "resultado" com os valores retornados por esta requisicao;  

15. Caso não tenha nenhum erro, então;  
    - Inserir um registro na tabela transferencia ("idconta_origem", "idconta_destino", "datamvto" e "valor");  
    - Atualizar o registro da tabela idempotencia (cujo "requisicao"="id_requisicao"") para: "status"="true" e preencher "status_code" e "resultado" com os valores retornados por esta requisicao;  
    STATUS_CODE_204_NO_CONTENT  



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

### TYPEs - Presentes no Enunciado:  
- TYPE_INVALID_DOCUMENT- Quando o documento for inválido (por exemplo, o CPF).  
- TYPE_INVALID_ACCOUNT - Quando a conta corrente for inválida.  
- TYPE_INACTIVE_ACCOUNT - Quando a conta corrente estiver inativa.  
- TYPE_INVALID_VALUE - Quando algum valor inválido for recebido via parâmetro/body.  
- TYPE_INVALID_TYPE - Quando for identificado alguma divergência em tipos de valores.  
- TYPE_USER_UNAUTHORIZED - Quando o usuário atutenticado não tem permissão para executar alguma ação.  
- TYPE_USER_AUTHORIZED - Quando o usuário efetua uma autenticação via login.  

### TYPEs - Minhas sugestões:  
- TYPE_ALREADY_EXISTS - Quando algum registro a ser cadastrado já estiver cadastrado (exemplo, um CPF que já está cadastrado).  
- TYPE_OPERATION_NOT_ALLOWED - Quando o usuário atutenticado não tem permissão para executar alguma ação.  
- TYPE_NOT_FOUND - Quando algum valor válido for recebido via parâmetro/body e não existe no banco para a operação (ex.: uma conta corrente (formato válido) for enviada).  
- TYPE_SUCCESS - Quando uma operação for bem sucedida.  

---




