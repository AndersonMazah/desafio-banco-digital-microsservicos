# Este documento contém minhas anotações referentes a SITUAÇÕES e DECISÕES enfrentadas.

---

## Microsserviço "usuário", que não está previsto:

**SITUAÇÃO**: O time de segurança informou que os dados de CPF e número da conta não podem transitar fora do microsserviço de usuário, porém, não está previsto este microsserviço, e sim, está previsto que os dados de usuário devem ficar no microsserviço "contacorrete".

**DECISÃO**: Vou considerar que o time de segurança desconhece detalhes dos microsserviços (nomes deles) ou até que, o time se confundiou, e trabalhar este requisito como, aplicável apenas no microsserviço de contacorrente.

Obs.: Em situação real, o ideal é consultar o time de segurança antes de tomar esta decisão.

---

## Definição do Banco de Dados:

**SITUAÇÃO 1**: No desenho de arquitetura, informa-se que cada API tem seu próprio banco de dados. Porém, também consta que "o banco" deve estar em um container. Nesse caso, há uma contradição: ou o desenho está incorreto ao indicar um banco para cada API, ou a informação de que "o banco" deve rodar em um container não está clara. Se houver mais de um banco, o correto seria indicar que cada banco deve rodar em um container.  
**DECISÃO 1**: Embora exista a informação de que o banco deve rodar em um container, entendo que o mais adequado seria que cada microsserviço tivesse seu próprio banco de dados, sendo assim, cada um dos bancos deve rodar em seu próprio container.  

**SITUAÇÃO 2**: A empresa usa Oracle e o enunciado sugere SQLite. SQLite é um banco embutido em arquivo, não um banco servidor, então não se encaixa bem em um container exclusivo de banco com porta, como no caso do Oracle. Além disso, meu computador não suporta rodar múltiplas instâncias de Oracle em containers.  
**DECISÃO 2**: Entendo que o banco não é o ponto central do desafio, já que o enunciado sugere SQLite. Vou utilizar o PostgreSQL por ser um banco servidor relacional, fácil de executar via Docker, e mais próximo do Oracle do que SQLite.  

---

## API Conta-Corrente:

**SITUAÇÃO 1**: Na tabela **contacorrente** existe o campo **nome**, mas este campo não está previsto para ser recebido no post de cadastro de conta corrente.  
**DECISÃO 1**: Incluir o campo **nome** no body deste endpoint.  

**SITUAÇÃO 2**: No endpoint de cadastro de conta-corrente, não está previsto validação de CPF, se já está cadastrado.  
**DECISÃO 2**: Incluir validação no cadastro de conta-corrente, para validar se o CPF já está cadastrado no banco.  

**SITUAÇÃO 3**: Não há informação sobre o tipo/formato do campo **número da conta**.  
**DECISÃO 3**: Optei por definir o numero da conta como um inteiro (bigint no banco e long no back, ou seja, 8 bytes). Este número de conta será gerenciado pelo campo numero da tabela, que será do tipo identity e unique.  

---




