# Este documento contém minhas anotações referentes a SITUAÇÕES e DECISÕES enfrentadas.

---

## Time de Segurança:  

**SITUAÇÃO 1**: O time de segurança informou que os dados de CPF e número da conta não podem transitar fora do microsserviço de usuário, porém, não está previsto este microsserviço, e sim, está previsto que os dados de usuário devem ficar no microsserviço "contacorrete".  
**DECISÃO 1**: Vou considerar que o time de segurança desconhece detalhes dos microsserviços (nomes deles) ou até que, o time se confundiou, e trabalhar este requisito como, aplicável apenas no microsserviço de contacorrente.  

**SITUAÇÃO 2**: O time de segurança informou que todos os endpoints devem estar protegidos com autenticação via JWT (nenhum endpoint pode ser acessdo sem token válido). Mas com isso, existe um problema: Se todos os endpoints devem estar protegidos, como se dará o cadastro do primeiro usuário?  
**DECISÃO 2**: Manter apenas 2 endpoints públicos, o de **cadastro** e o de **login**. O endpoint de cadastro permite que um novo usuário se registre informando seus dados sem exigir JWT, e o de login, também sem exigir token, valida as credenciais e retorna um JWT quando estiverem corretas.  

---

## Definição do Banco de Dados:

**SITUAÇÃO 1**: No desenho de arquitetura, informa-se que cada API tem seu próprio banco de dados. Porém, também consta que "o banco" deve estar em um container (ou seja, um banco). Nesse caso, há uma contradição: ou o desenho está incorreto ao indicar um banco para cada API, ou a informação de que "o banco" deve rodar em um container não está clara. Se houver mais de um banco, o correto seria indicar que cada banco deve rodar em um container.  
**DECISÃO 1**: Vou seguir com o entendimento de que "o banco" (um banco) apenas deve existir e não um banco por api.  

**SITUAÇÃO 2**: A empresa usa Oracle e o enunciado sugere SQLite.  
**DECISÃO 2**: Entendo que o banco de dados não é o ponto central do desafio e que não há uma exigência específica, apenas uma sugestão. Por isso, vou utilizar o PostgreSQL, pois ele é mais próximo do Oracle do que o SQLite.  

---

## API Conta-Corrente:

**SITUAÇÃO 1**: Na tabela **contacorrente** existe o campo **nome**, mas este campo não está previsto para ser recebido no post de cadastro de conta corrente.  
**DECISÃO 1**: Incluir o campo **nome** no body deste endpoint.  

**SITUAÇÃO 2**: No endpoint de cadastro de conta-corrente, não está previsto validação de CPF, se já está cadastrado.  
**DECISÃO 2**: Incluir validação no cadastro de conta-corrente, para validar se o CPF já está cadastrado no banco.  

**SITUAÇÃO 3**: Não há informação sobre o tipo/formato do campo **número da conta**.  
**DECISÃO 3**: Optei por definir o numero da conta como um inteiro (bigint no banco e long no back, ou seja, 8 bytes). Este número de conta será gerenciado pelo campo numero da tabela, que será do tipo identity e unique.  

**SITUAÇÃO 4**: Não há informação sobre o tamanho do campo nome, nem sobre o tamanho e as regras aplicáveis ao campo senha.  
**DECISÃO 4**: Optei por definir o campo nome com tamanho mínimo de 1 e máximo de 120 caracteres. O campo senha foi definido com tamanho fixo de 6 caracteres, mínimo e máximo de 6 caracteres, sem necessidade de regras adicionais.  

**SITUAÇÃO 5**: No endpoint de Inativar, na regra que valida se o usuário está com token válido ou expirado, está sendo utilizado o status code 403 como retorno. Contudo, o correto para esse caso é retornar o status code 401, pois se trata de falha de autenticação e não de autorização.  
**DECISÃO 5**: Decidi utilizar o status code 401 para essa validação, a fim de manter a coerência com o padrão REST.  

**SITUAÇÃO 6**: Na tabela idempotencia não existe um campo de status para indicar requisições em processamento. Com isso, entende-se que o registro só será persistido ao final da transação, o que pode permitir que duas chamadas ao mesmo endpoint sejam executadas simultaneamente antes da gravação do registro de idempotência.  
**DECISÃO 6**: Embora o enunciado não trate desse ponto, entendo que seria adequado incluir um campo de status na tabela idempotencia. Dessa forma, ao receber uma nova chamada, o sistema poderia verificar se já existe um registro para aquela requisição. Caso não exista, o registro seria criado com o status 1 = início. Se uma segunda chamada ocorrer antes do término do processamento, o sistema identificará que a mesma requisição já está em andamento, evitando a necessidade de tratar rollback caso a segunda tentativa falhe ao inserir o mesmo id_requisicao ao final do processamento.  

**SITUAÇÃO 7**: Na tabela idempotencia não existe um campo de status_code, e o nome do campo da cheve primária está como "chave_" (diferente do padrão de nome das demais tabelas).  
**DECISÃO 7**: Decidi incluir o campo "status_code" (string de 3 caracteres) e renomear o campo da chave primária para "ididempotencia"  

**SITUAÇÃO 8**: O time de segurança solicitou que os dados de usuário não transitem entre as APIs. Com isso, torna-se impossível implementar o endpoint de transferência, pois ele precisaria receber o número da conta e encaminhá-lo para a API de movimento.  
**DECISÃO 8**: A solução é criar um endpoint para consulta de CPF. Esse fluxo é semelhante ao utilizado em aplicativos bancários quando se realiza uma transferência via chave Pix CPF: o usuário informa a chave e, em seguida, é exibido o primeiro nome do cliente para confirmação antes de prosseguir com a operação. Assim, optei por incluir um novo endpoint de consulta que recebe um CPF como parâmetro e retorna ao front apenas o primeiro nome do cliente e o UUID da conta. Com essas informações, o front poderá chamar o endpoint de transferência utilizando o UUID da conta de destino, sem a necessidade de enviar o número da conta do cliente.  
obs.: O endpoint de consultar CPF usará o verbo POST ao invés de GET, pois entendo ser mais seguro para não deixar informações de clientes serem rastreadas pela rede.  

---

## API Transferência:

**SITUAÇÃO 1**: No enunciado fala que a api de transferencia deve receber o "número da conta de destino" e existe uma restrição de que não se pode transitar o número de conta entre as api.  
**DECISÃO 1**: Com isso, decidi que o tipo de parâmetro não será mais o "número da conta destino" e sim o "uuid da conta de destino".  

---




