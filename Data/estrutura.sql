CREATE TABLE IF NOT EXISTS usuarios (
 id INT AUTO_INCREMENT PRIMARY KEY, nome VARCHAR(120) NOT NULL, usuario VARCHAR(60) NOT NULL UNIQUE,
 senha VARCHAR(500) NOT NULL, email VARCHAR(180), ativo BOOLEAN NOT NULL DEFAULT TRUE,
 administrador BOOLEAN NOT NULL DEFAULT FALSE, versao_sessao INT NOT NULL DEFAULT 0,
 data_cadastro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, data_alteracao DATETIME NULL
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS projetos (
 id INT AUTO_INCREMENT PRIMARY KEY, usuario_id INT NOT NULL, nome VARCHAR(120) NOT NULL,
 FOREIGN KEY (usuario_id) REFERENCES usuarios(id), UNIQUE KEY uq_projeto(usuario_id,nome)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS demandas (
 id INT AUTO_INCREMENT PRIMARY KEY, usuario_id INT NOT NULL, projeto_id INT NULL,
 titulo VARCHAR(180) NOT NULL, descricao TEXT, prioridade VARCHAR(20) NOT NULL, status VARCHAR(25) NOT NULL,
 data_recebimento DATETIME NOT NULL, prazo DATETIME NULL, observacoes TEXT, data_finalizacao DATETIME NULL,
 data_cadastro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
 FOREIGN KEY(usuario_id) REFERENCES usuarios(id), FOREIGN KEY(projeto_id) REFERENCES projetos(id),
 INDEX ix_demandas_usuario_status(usuario_id,status)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS apontamentos (
 id INT AUTO_INCREMENT PRIMARY KEY, demanda_id INT NOT NULL, usuario_id INT NOT NULL,
 inicio DATETIME NOT NULL, fim DATETIME NULL, descricao TEXT, lancamento_manual BOOLEAN NOT NULL DEFAULT FALSE,
 usuario_em_execucao INT GENERATED ALWAYS AS (CASE WHEN fim IS NULL THEN usuario_id ELSE NULL END) STORED,
 UNIQUE KEY uq_apontamento_ativo(usuario_em_execucao),
 FOREIGN KEY(demanda_id) REFERENCES demandas(id), FOREIGN KEY(usuario_id) REFERENCES usuarios(id),
 INDEX ix_apontamentos_periodo(usuario_id,inicio,fim), INDEX ix_apontamentos_demanda(demanda_id),
 CONSTRAINT ck_intervalo CHECK (fim IS NULL OR fim >= inicio)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS pausas_apontamentos (
 id INT AUTO_INCREMENT PRIMARY KEY, apontamento_id INT NOT NULL, inicio DATETIME NOT NULL, fim DATETIME NULL,
 FOREIGN KEY(apontamento_id) REFERENCES apontamentos(id)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS pagamentos (
 demanda_id INT NULL, CONSTRAINT fk_pagamentos_demanda FOREIGN KEY(demanda_id) REFERENCES demandas(id),
 id INT AUTO_INCREMENT PRIMARY KEY, usuario_id INT NOT NULL, data_pagamento DATETIME NOT NULL,
 periodo_inicio DATETIME NOT NULL, periodo_fim DATETIME NOT NULL, valor_pago DECIMAL(10,2) NOT NULL,
 observacao TEXT, FOREIGN KEY(usuario_id) REFERENCES usuarios(id), INDEX ix_pagamentos_periodo(usuario_id,periodo_inicio,periodo_fim)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS configuracoes (
 usuario_id INT PRIMARY KEY, valor_hora DECIMAL(10,2) NULL, FOREIGN KEY(usuario_id) REFERENCES usuarios(id)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS historico_demandas (
 id INT AUTO_INCREMENT PRIMARY KEY, usuario_id INT NOT NULL, demanda_id INT NULL,
 data DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, acao VARCHAR(180) NOT NULL, detalhes TEXT,
 FOREIGN KEY(usuario_id) REFERENCES usuarios(id), FOREIGN KEY(demanda_id) REFERENCES demandas(id), INDEX ix_historico(usuario_id,data)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS whatsapp_contatos (
 id INT AUTO_INCREMENT PRIMARY KEY, usuario_id INT NOT NULL, nome VARCHAR(120) NOT NULL,
 telefone VARCHAR(20) NOT NULL, template TEXT NOT NULL,
 FOREIGN KEY(usuario_id) REFERENCES usuarios(id), INDEX ix_whatsapp_contatos(usuario_id,nome)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS whatsapp_conexoes (
 usuario_id INT PRIMARY KEY, instancia VARCHAR(100) NOT NULL UNIQUE,
 FOREIGN KEY(usuario_id) REFERENCES usuarios(id)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS whatsapp_envios (
 id BIGINT AUTO_INCREMENT PRIMARY KEY, usuario_id INT NOT NULL, chave CHAR(36) NOT NULL,
 nome VARCHAR(120) NOT NULL, telefone VARCHAR(20) NOT NULL, template TEXT NOT NULL, mensagem TEXT NOT NULL,
 instancia VARCHAR(100) NOT NULL, tipo VARCHAR(10) NOT NULL, inicio DATE NOT NULL, fim DATE NOT NULL,
 demanda_id INT NULL, filtro_status VARCHAR(25) NULL,
 nome_arquivo VARCHAR(180) NOT NULL, mime VARCHAR(120) NOT NULL, arquivo MEDIUMBLOB NOT NULL,
 status VARCHAR(20) NOT NULL, evolution_id VARCHAR(200) NULL, erro VARCHAR(500) NULL,
 criado_em DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()), finalizado_em DATETIME NULL,
 FOREIGN KEY(usuario_id) REFERENCES usuarios(id), UNIQUE KEY uq_whatsapp_envio(usuario_id,chave),
 INDEX ix_whatsapp_envios(usuario_id,id)
) ENGINE=InnoDB;

