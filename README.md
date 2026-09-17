# WorkIT — Painel do Desenvolvedor & Buscador de Vagas TI

Aplicação desktop completa desenvolvida em **C# (.NET / WPF)** com banco local **SQLite**, consumo assíncrono da **API pública da Gupy**, classificação automática de níveis de senioridade, sistema de favoritos e exportação para Excel (CSV).

Projeto desenvolvido para a disciplina de **Desenvolvimento de Software Visual**.

---

## 🚀 Como Rodar o Projeto

### Opção 1: 1 Clique com o Arquivo "Executar.bat"
Basta dar duplo clique no arquivo `Executar.bat` na raiz do projeto. Ele detecta automaticamente o executável Release compilado ou roda via .NET.

### Opção 2: Via Terminal (.NET SDK)
Abra o terminal na pasta `WorkIT.App` e execute:
```bash
dotnet run
```

### Opção 3: Executável Direto
Você também pode rodar o arquivo compilado diretamente sem precisar do terminal:
`WorkIT.App\bin\Release\net10.0-windows\WorkIT.App.exe`

---

## 🛠️ Tecnologias e Bibliotecas

| Tecnologia | Finalidade no Projeto |
| :--- | :--- |
| **C# (.NET 10 / .NET Desktop)** | Linguagem principal do backend e regras de negócio |
| **WPF (Windows Presentation Foundation)** | Interface gráfica visual moderna com tema escuro nativo (Dark Theme) |
| **HttpClient & System.Text.Json** | Consumo assíncrono da API REST da Gupy e desserialização de DTOs |
| **Microsoft.Data.Sqlite** | Persistência em banco local (`vagas.db`) com idempotência |
| **ProcessStartInfo** | Abertura ágil dos links de vagas no navegador padrão do SO |
| **ExportService (CSV / BOM)** | Exportação formatada para leitura nativa no Microsoft Excel |

---

## 📂 Estrutura de Pastas e Código

```text
WorkIT.App/
│
├── Models/                     # Classes de Dados e DTOs
│   ├── Vaga.cs                 # Modelo principal da vaga (com badges e formatadores)
│   ├── GupyApiResponse.cs      # Mapeamento do JSON retornado pela API da Gupy
│   └── AreaModel.cs            # Modelos para áreas de TI e estados brasileiros (UFs)
│
├── Services/                   # Regras de Negócio e Serviços
│   ├── GupyApiService.cs       # Consumo da API REST pública com paginação e filtros
│   ├── VagaClassifierService.cs# Classificação de níveis (Estágio, Jr, Pleno, Sr) e UFs
│   └── ExportService.cs        # Exportação para CSV compatível com Excel (UTF-8 BOM)
│
├── Data/                       # Camada de Persistência
│   └── DatabaseService.cs      # Conexão SQLite (vagas.db), INSERT OR IGNORE e favoritos
│
├── MainWindow.xaml             # Interface gráfica modular em abas com tema Dark
├── MainWindow.xaml.cs          # Conexão entre eventos visuais e chamadas assíncronas
└── WorkIT.App.csproj           # Arquivo de configuração e dependências do projeto
```

---

## ✨ Funcionalidades do Sistema

1. **Filtros Dinâmicos:**
   * 11 áreas de TI mapeadas (RPA, Fullstack, Frontend, Backend, Cybersecurity, Dados/BI, DevOps, Mobile, QA, Redes, IA) + opção "Todas as Áreas".
   * Dropdown com os 27 estados do Brasil (UFs).
   * Filtro textual de Cidade e Palavra-chave adicional.
2. **Consumo da API Pública:**
   * Requisições HTTP automáticas com controle de paginação.
   * Não exige chaves pagas nem configurações de proxy.
3. **Detecção Inteligente de Nível:**
   * Reconhecimento automático no título da vaga de: *Estágio*, *Júnior*, *Pleno* e *Sênior*.
4. **Persistência SQLite com Detecção de Novidades:**
   * Inserção com `INSERT OR IGNORE` usando o ID da vaga.
   * Diferenciação visual na tela: `[NOVA]` (verde) vs `[JÁ VISTA]` (cinza).
5. **Sistema de Favoritos:**
   * Salva vagas de interesse no banco local para acompanhamento do processo seletivo.
6. **Abertura Rápida de Links:**
   * Abre o link oficial da candidatura diretamente no navegador padrão com um clique.
7. **Exportação para Excel (CSV):**
   * Exporta a lista de vagas filtradas em formato CSV com codificação correta para o Excel.

---

## 🎓 Guia de Apresentação e Defesa para o Grupo (6 Integrantes)

Para que todos os integrantes tenham uma parte clara e técnica para defender perante o professor:

* **Integrante 1 (Arquitetura e DTOs):**
  * Explica a divisão do projeto em camadas (`Models`, `Services`, `Data`, `UI`).
  * Mostra as classes `Vaga.cs` e `GupyApiResponse.cs`, explicando como o C# mapeia o JSON externo para objetos fortemente tipados.

* **Integrante 2 (Integração com API REST - `GupyApiService.cs`):**
  * Explica o uso de `HttpClient`, parâmetros de URL (`jobName`, `limit`, `offset`), chamadas assíncronas (`async/await`) e tratamento de erros de rede.

* **Integrante 3 (Lógica de Negócio e Classificação - `VagaClassifierService.cs`):**
  * Demonstra como o sistema classifica a senioridade (Estágio, Júnior, Pleno, Sênior) e normaliza acentos com `UnicodeCategory` para permitir buscas por cidades sem sensibilidade a diacríticos.

* **Integrante 4 (Banco de Dados SQLite - `DatabaseService.cs`):**
  * Apresenta o arquivo `vagas.db`, a tabela `vagas`, o comando `INSERT OR IGNORE` para evitar duplicatas e como o sistema sabe se a vaga é `[NOVA]` ou `[JÁ VISTA]`.

* **Integrante 5 (Interface Visual WPF - `MainWindow.xaml`):**
  * Mostra a construção da tela em XAML, o layout responsivo em abas (Oportunidades, Favoritas, Estatísticas), os cards com bindings visuais e o tema escuro.

* **Integrante 6 (Ações do Usuário, Exportação CSV e Demonstração Prática):**
  * Executa a aplicação ao vivo, faz uma busca por vagas, abre o link no navegador via `ProcessStartInfo`, favorita uma vaga e demonstra a exportação para o Excel.
