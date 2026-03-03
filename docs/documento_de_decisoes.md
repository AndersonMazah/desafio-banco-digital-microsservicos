# Este documento contém minhas anotações referentes a SITUAÇÕES e DECISÕES enfrentadas.

---

## Time de Segurança:  

**SITUAÇÃO 1**: O time de segurança informou que os dados de CPF e número da conta não podem transitar fora do microsserviço de usuário, porém, não está previsto este microsserviço, e sim, está previsto que os dados de usuário devem ficar no microsserviço "contacorrete".  
**DECISÃO 1**: Vou considerar que o time de segurança desconhece detalhes dos microsserviços (nomes deles) ou até que, o time se confundiou, e trabalhar este requisito como, aplicável apenas no microsserviço de contacorrente.  

**SITUAÇÃO 2**: O time de segurança informou que todos os endpoints devem estar protegidos com autenticação via JWT (nenhum endpoint pode ser acessdo sem token válido). Mas com isso, existe um problema: Se todos os endpoints devem estar protegidos, como se dará o cadastro do primeiro usuário?  
**DECISÃO 2**: Manter apenas 2 endpoints públicos, o de **cadastro** e o de **login**. O endpoint de cadastro permite que um novo usuário se registre informando seus dados sem exigir JWT, e o de login, também sem exigir token, valida as credenciais e retorna um JWT quando estiverem corretas.  

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

**SITUAÇÃO 4**: No endpoint de Inativar, na regra que valida se o usuário está com token válido ou expirado, está sendo utilizado o status code 403 como retorno. Contudo, o correto para esse caso é retornar o status code 401, pois se trata de falha de autenticação e não de autorização.  
**DECISÃO 4**: Decidi utilizar o status code 401 para essa validação, a fim de manter a coerência com o padrão REST.  


---




