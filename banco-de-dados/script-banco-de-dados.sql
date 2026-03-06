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


