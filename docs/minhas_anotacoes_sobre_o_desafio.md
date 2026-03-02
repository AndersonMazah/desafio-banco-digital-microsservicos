# Este documento contém minhas anotações referente a como eu fiz a leitura e entendimento deste desafio.

---

## Conflito de informação sobre nome do microsserviço "usuário" que não está previsto

**SITUAÇÃO**: O time de segurança informou que os dados de CPF e número da conta não podem transitar fora do microsserviço de usuário, porém, não está previsto este microsserviço, e sim, está previsto que os dados de usuário devem ficar no microsserviço "contacorrete".  

**DECISÃO**: Vou considerar que o time de segurança desconhece detalhes dos microsserviços (nomes deles) ou até que, o time se confundiou, e trabalhar este requisito como, aplicável apenas no microsserviço de contacorrente.  

Obs.: Em situação real, o ideal é consultar o time de segurança antes de tomar esta decisão.

---
