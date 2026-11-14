# Core

Serviços singleton da aplicação, como autenticação, sessão, interceptores HTTP e
configuração de API. A sessão da F1 mantém o token de acesso apenas em memória e
renova o refresh de forma compartilhada entre requisições concorrentes.

Regras: sem regra de negócio duplicada da API, sem token em `localStorage` e sem
dados pessoais em cache persistente.
