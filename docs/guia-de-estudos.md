# Guia de Estudos — Lógica de Programação, C# e Estruturas de Dados

Este guia usa o código já existente no FinLedger como material de estudo. Ele não repete o
conteúdo de [`database-patterns.md`](database-patterns.md) (esse é focado em padrões de banco) —
aqui o foco é: como o código *pensa* (lógica), como o C# é usado (linguagem) e onde estruturas de
dados aparecem (ou deveriam ser praticadas) no projeto.

Cada ponto tem um arquivo e linha exatos para você abrir e ler no contexto real, não isolado.

---

## Como estudar este projeto (ordem sugerida)

O projeto segue Clean Architecture, e a ordem de leitura recomendada segue o fluxo de uma
requisição, de dentro para fora — é também a ordem mais didática:

1. **Domain** (`backend/src/FinLedger.Domain/`) — as regras de negócio puras, sem framework.
2. **Application** (`backend/src/FinLedger.Application/`) — os casos de uso (comandos/queries).
3. **Infrastructure** (`backend/src/FinLedger.Infrastructure/`) — como isso é persistido.
4. **API** (`backend/src/FinLedger.API/`) — como isso vira HTTP.
5. **Tests** (`backend/tests/`) — como tudo isso é verificado.

Essa ordem importa: o Domain não conhece nada das outras camadas (é só C# puro), então é o lugar
mais fácil para começar a ler sem se perder em detalhes de EF Core, Docker, etc.

---

## Parte 1 — Lógica de Programação

### 1.1 Guard clauses / fail-fast com `?? throw`
[`DepositCommandHandler.cs:28-29`](../backend/src/FinLedger.Application/Transactions/Commands/Deposit/DepositCommandHandler.cs#L28-L29), repetido em `WithdrawCommandHandler.cs`, `CloseAccountCommandHandler.cs`, `TransferCommandHandler.cs:64-67`.

```csharp
var account = await _accounts.GetByIdAsync(request.AccountId, ct)
    ?? throw new AccountNotFoundException(request.AccountId);
```
Em vez de `if (account == null) { throw ... } ` em três linhas, o operador `??` (null-coalescing)
lança a exceção inline. É o mesmo princípio de "falhe cedo, falhe alto" — o método não continua
com um estado inválido (`account` nulo) escondido.

**Pergunta para você**: reescreva essa linha usando `if` tradicional. Depois compare legibilidade.

### 1.2 Regra de negócio vive dentro do objeto, não fora dele
[`Account.cs:39-53`](../backend/src/FinLedger.Domain/Entities/Account.cs#L39-L53):

```csharp
public void Credit(decimal amount) {
    if (amount <= 0) throw new ArgumentException(...);
    Balance += amount;
}
public void Debit(decimal amount) {
    if (amount <= 0) throw new ArgumentException(...);
    if (Balance < amount) throw new InsufficientFundsException(...);
    Balance -= amount;
}
```
Repare que `Balance` só tem `private set` (linha 12) — ninguém de fora pode fazer
`account.Balance = -500`. A única forma de mudar o saldo é chamando `Credit`/`Debit`, que sempre
validam. Isso é "tell, don't ask": em vez de perguntar o saldo e decidir de fora, você manda o
objeto se atualizar e ele decide se pode.

**Contraste**: veja como isso seria mais frágil se `Balance` tivesse `set` público e a validação
ficasse espalhada em cada handler que precisa mexer em saldo.

### 1.3 Validação em camadas (defesa em profundidade)
Mesma regra ("valor deve ser positivo") é verificada em **três lugares diferentes**, de propósito:

1. Entrada HTTP: [`CreateAccountCommandValidator.cs:9`](../backend/src/FinLedger.Application/Accounts/Commands/CreateAccount/CreateAccountCommandValidator.cs#L9) / [`TransferCommandValidator.cs:14`](../backend/src/FinLedger.Application/Transactions/Commands/Transfer/TransferCommandValidator.cs#L14) — FluentValidation, roda antes do handler.
2. Regra de domínio: [`Account.cs:41`](../backend/src/FinLedger.Domain/Entities/Account.cs#L41) — `if (amount <= 0) throw`.
3. Banco de dados: [`TransactionConfiguration.cs:17`](../backend/src/FinLedger.Infrastructure/Persistence/Configurations/TransactionConfiguration.cs#L17) — `CHECK (amount > 0)`.

**Por que repetir a mesma validação três vezes não é redundância burra**: cada camada protege
contra um cenário diferente — (1) protege a UX (erro amigável, rápido); (2) protege o domínio
mesmo se alguém pular a validação de entrada (outro handler, outro caller); (3) protege o banco
mesmo se alguém rodar um `INSERT` manual ou um bug passar pelas duas primeiras camadas.

### 1.4 Ordenação determinística para evitar deadlock
[`TransferCommandHandler.cs:54-57`](../backend/src/FinLedger.Application/Transactions/Commands/Transfer/TransferCommandHandler.cs#L54-L57):

```csharp
var (firstId, secondId) = request.SourceAccountId.CompareTo(request.DestinationAccountId) < 0
    ? (request.SourceAccountId, request.DestinationAccountId)
    : (request.DestinationAccountId, request.SourceAccountId);
```
Isso é lógica pura, sem nada de banco: **duas transferências simultâneas** `A→B` e `B→A`
tentando travar suas próprias contas de origem primeiro podem travar uma esperando a outra para
sempre (deadlock). A solução é sempre pedir os locks na mesma ordem (aqui, pelo Guid menor),
não importa quem é origem/destino — assim as duas transferências disputam o mesmo primeiro lock,
e uma delas simplesmente espera a outra terminar.

**Exercício**: no papel, simule `Transfer(A→B, valor=10)` e `Transfer(B→A, valor=5)` rodando ao
mesmo tempo, primeiro SEM a ordenação (cada uma trava sua origem primeiro) e depois COM a
ordenação. Desenhe a linha do tempo dos locks.

### 1.5 `switch` expression para mapear estado → ação
[`AuditInterceptor.cs:60-66`](../backend/src/FinLedger.Infrastructure/Persistence/Interceptors/AuditInterceptor.cs#L60-L66):

```csharp
var action = entry.State switch {
    EntityState.Added    => "INSERT",
    EntityState.Modified => "UPDATE",
    EntityState.Deleted  => "DELETE",
    _                    => "UNKNOWN"
};
```
Isso é uma tabela de decisão sem `if/else if/else if`. O `_` é o caso "senão" (igual ao `default`
de um `switch` tradicional). Compare com o `switch` statement (com `case`/`break`) que você talvez
já conheça — aqui é uma **expressão** que retorna valor direto.

### 1.6 A jornada de uma exceção através das camadas
Siga esta exceção por três arquivos, na ordem que ela realmente acontece:

1. **Lançada** em [`UnitOfWork.cs:35-41`](../backend/src/FinLedger.Infrastructure/Persistence/UnitOfWork.cs#L35-L41): o EF Core lança `DbUpdateConcurrencyException` (genérica, de infraestrutura); o `UnitOfWork` a intercepta e relança como `ConcurrencyConflictException` (do domínio).
2. **Por quê a troca?** Documentado no próprio código: "a camada de Application fica livre de dependências de infraestrutura" — Application nunca vê `DbUpdateConcurrencyException`, só vê exceções que o próprio Domain define.
3. **Capturada** em [`ExceptionHandlingMiddleware.cs:48-54`](../backend/src/FinLedger.API/Middleware/ExceptionHandlingMiddleware.cs#L48-L54): vira HTTP 409 Conflict.

**Exercício**: faça o mesmo mapeamento para `InsufficientFundsException` — onde nasce
([`Account.cs:51`](../backend/src/FinLedger.Domain/Entities/Account.cs#L51)), por onde passa sem
ser capturada (`WithdrawCommandHandler.cs:33`, comentário na linha 32 diz isso explicitamente), e
onde vira HTTP 422 ([`ExceptionHandlingMiddleware.cs:37-47`](../backend/src/FinLedger.API/Middleware/ExceptionHandlingMiddleware.cs#L37-L47)).

### 1.7 Discussão em aberto: idempotência
Nenhum endpoint deste projeto é idempotente hoje: se o cliente reenviar um `POST /deposit` porque
a resposta demorou (mas o servidor já processou), o depósito acontece **duas vezes**.
**Exercício de design** (não precisa implementar): como você adicionaria uma `Idempotency-Key` ao
`DepositCommand` para que reenviar a mesma requisição não duplique o efeito? (Dica: pesquise
"idempotency key pattern" — é assim que APIs de pagamento reais, tipo Stripe, resolvem isso.)

---

## Parte 2 — C# (recursos de linguagem usados no projeto)

| Recurso | Onde ver | O que observar |
|---|---|---|
| Namespace de arquivo único (`namespace X;` sem chaves) | Todo arquivo, ex. [`Account.cs:1`](../backend/src/FinLedger.Domain/Entities/Account.cs#L1) | Sintaxe C# 10+, elimina um nível de indentação |
| `sealed class` | [`Account.cs:6`](../backend/src/FinLedger.Domain/Entities/Account.cs#L6) | Proíbe herança — em DDD, impede que alguém "escape" das invariantes criando uma subclasse |
| Construtor privado + fábrica estática | [`Account.cs:27-37`](../backend/src/FinLedger.Domain/Entities/Account.cs#L27-L37) | `Account.Create(...)` é a única forma de instanciar — impossível criar um `Account` em estado inválido |
| Auto-propriedade com `private set` | [`Account.cs:8-13`](../backend/src/FinLedger.Domain/Entities/Account.cs#L8-L13) | Leitura pública, escrita só de dentro da classe |
| Nullable reference types | `string? Description`, `Guid? CounterpartyAccountId` em [`Transaction.cs:12,15`](../backend/src/FinLedger.Domain/Entities/Transaction.cs#L12) | O compilador avisa se você esquecer de checar null |
| Records | [`TransferCommand.cs:5-11`](../backend/src/FinLedger.Application/Transactions/Commands/Transfer/TransferCommand.cs#L5-L11), [`PagedResult.cs:3-12`](../backend/src/FinLedger.Application/Common/Models/PagedResult.cs#L3-L12) | Tipo imutável, igualdade por valor (não por referência), sintaxe posicional |
| Record com propriedades computadas | [`PagedResult.cs:9-11`](../backend/src/FinLedger.Application/Common/Models/PagedResult.cs#L9-L11) | `TotalPages`, `HasNextPage` — expression-bodied properties dentro de um record |
| Generics | `PagedResult<T>`, `IRequestHandler<TRequest,TResponse>` em toda handler | Um tipo/contrato que funciona para qualquer `T` |
| Tupla + desconstrução | [`TransferCommandHandler.cs:55-57,69-71`](../backend/src/FinLedger.Application/Transactions/Commands/Transfer/TransferCommandHandler.cs#L55-L57) | `var (a, b) = condição ? (x, y) : (y, x);` — dois valores relacionados sem criar uma classe |
| Collection expression (C# 12) | [`TransferCommandHandler.cs:91`](../backend/src/FinLedger.Application/Transactions/Commands/Transfer/TransferCommandHandler.cs#L91) — `[debitTx, creditTx]` | Sintaxe nova para literais de coleção, substitui `new[] { ... }` |
| Raw string literal (`"""`) | [`StatementRepository.cs:28-66`](../backend/src/FinLedger.Infrastructure/Persistence/Repositories/StatementRepository.cs#L28-L66) | SQL multi-linha sem escapar aspas nem `\n` |
| LINQ | [`GetAllAccountsQueryHandler.cs:16-18`](../backend/src/FinLedger.Application/Accounts/Queries/GetAllAccounts/GetAllAccountsQueryHandler.cs#L16-L18) (`.Select`), [`AccountRepository.cs:31-33`](../backend/src/FinLedger.Infrastructure/Persistence/Repositories/AccountRepository.cs#L31-L33) (`.Where().OrderByDescending()`) | Consultas declarativas sobre coleções |
| `async`/`await` + `CancellationToken` | Em **toda** assinatura de handler/repositório | O `ct` é passado adiante em cada chamada — permite cancelar a operação inteira (ex: cliente fechou a conexão) |
| `IAsyncDisposable` / `await using` | [`TransferCommandHandler.cs:61`](../backend/src/FinLedger.Application/Transactions/Commands/Transfer/TransferCommandHandler.cs#L61), [`IUnitOfWork.cs:11-14`](../backend/src/FinLedger.Application/Common/Interfaces/IUnitOfWork.cs#L11-L14) | Garante rollback automático se uma exceção escapar do bloco, sem precisar de `try/finally` manual |
| Inversão de dependência via interface | [`IAccountRepository.cs`](../backend/src/FinLedger.Domain/Interfaces/IAccountRepository.cs) (Domain) implementado por [`AccountRepository.cs`](../backend/src/FinLedger.Infrastructure/Persistence/Repositories/AccountRepository.cs) (Infrastructure) | O comentário no próprio arquivo explica a inversão: "Infrastructure depende de Domain, nunca o contrário" |
| Método de extensão para DI | [`Infrastructure/DependencyInjection.cs:16`](../backend/src/FinLedger.Infrastructure/DependencyInjection.cs#L16) — `AddInfrastructure(this IServiceCollection...)` | Padrão usado por toda a comunidade .NET (`AddControllers()`, `AddDbContext()` são a mesma técnica) |
| Middleware customizado | [`ExceptionHandlingMiddleware.cs`](../backend/src/FinLedger.API/Middleware/ExceptionHandlingMiddleware.cs) | Convenção: construtor recebe `RequestDelegate next`, método `InvokeAsync(HttpContext)` |
| Pattern matching (`is not (A or B)`) | [`AuditInterceptor.cs:57`](../backend/src/FinLedger.Infrastructure/Persistence/Interceptors/AuditInterceptor.cs#L57) | `if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;` |
| Exceção customizada carregando dados | [`InsufficientFundsException.cs`](../backend/src/FinLedger.Domain/Exceptions/InsufficientFundsException.cs) | Guarda `AccountId`, `CurrentBalance`, `RequiredAmount` — o middleware usa esses campos para montar a resposta JSON, não só a mensagem |
| xUnit + FluentAssertions | [`AccountTests.cs`](../backend/tests/FinLedger.Tests/Unit/AccountTests.cs) | `[Fact]`, `.Should().Be(...)`, `.Should().Throw<T>()` — leia como se fosse português: "o saldo deveria ser 700" |
| Testcontainers | [`TransferIntegrationTests.cs:23-57`](../backend/tests/FinLedger.Tests/Integration/TransferIntegrationTests.cs#L23-L57) | Sobe um Postgres real em Docker por teste — `IAsyncLifetime` com `InitializeAsync`/`DisposeAsync` |

**Como estudar esta tabela**: não leia de cima a baixo de uma vez. Abra os handlers de
`Deposit`, `Withdraw` e `CreateAccount` (os três são quase idênticos e bem curtos) e ache
sozinho quantos desses recursos você reconhece sem consultar a tabela. Só depois confira o que
faltou.

---

## Parte 3 — Estruturas de Dados

**Seja honesto consigo mesmo aqui**: este projeto **não implementa** estruturas de dados
clássicas "na mão" (sem lista encadeada, árvore, heap ou grafo escritos manualmente). Ele usa as
coleções prontas do .NET (`List<T>`, `Dictionary<TKey,TValue>`) e delega o trabalho estrutural
pesado (indexação, busca eficiente) para o PostgreSQL. Isso é **realista** — é assim que se
programa no mundo real — mas significa que para estudar estrutura de dados de verdade, você vai
precisar completar o que o projeto não mostra. Veja a seção de exercícios abaixo.

### O que já aparece no código

1. **Lista / `IReadOnlyList<T>`** — usada como retorno em quase toda query, ex.
   [`IAccountRepository.cs:13`](../backend/src/FinLedger.Domain/Interfaces/IAccountRepository.cs#L13).
   O `IReadOnlyList<T>` em vez de `List<T>` é uma decisão de design: o chamador pode ler mas não
   pode adicionar/remover itens da coleção retornada.

2. **`Dictionary<TKey,TValue>` (tabela hash)** — [`AuditInterceptor.cs:78,85`](../backend/src/FinLedger.Infrastructure/Persistence/Interceptors/AuditInterceptor.cs#L78-L85)
   usa `.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue)` para montar um mapa
   nome-da-propriedade → valor antes de serializar em JSON. Outro exemplo:
   [`ExceptionHandlingMiddleware.cs:27-29`](../backend/src/FinLedger.API/Middleware/ExceptionHandlingMiddleware.cs#L27-L29)
   agrupa erros de validação por nome de campo. Em ambos os casos, a busca por chave é O(1) em
   média — é por isso que se usa `Dictionary` e não `List` + busca linear.

3. **Soma prefixada (prefix sum) — só que quem calcula é o banco, não o C#**
   [`StatementRepository.cs:41-51`](../backend/src/FinLedger.Infrastructure/Persistence/Repositories/StatementRepository.cs#L41-L51):
   a window function `SUM(...) OVER (PARTITION BY account_id ORDER BY created_at ASC ROWS BETWEEN
   UNBOUNDED PRECEDING AND CURRENT ROW)` é **exatamente** o algoritmo clássico de soma prefixada
   (prefix sum / running total) que se estuda em estrutura de dados — só que rodando dentro do
   PostgreSQL em vez de um `for` em C#.

4. **Índice B-Tree** (conceito, não implementação) —
   [`TransactionConfiguration.cs:41-42`](../backend/src/FinLedger.Infrastructure/Persistence/Configurations/TransactionConfiguration.cs#L41-L42)
   e [`AccountConfiguration.cs:43-45`](../backend/src/FinLedger.Infrastructure/Persistence/Configurations/AccountConfiguration.cs#L43-L45)
   criam índices. O PostgreSQL implementa isso como uma árvore B (B-Tree) por padrão — é a
   estrutura de dados que transforma uma busca O(n) (percorrer a tabela inteira) em uma busca
   O(log n). O projeto usa a árvore, mas não a implementa — vale a pena estudar B-Tree à parte.

5. **Versão como técnica de concorrência (parecido com Compare-And-Swap)** —
   [`AccountConfiguration.cs:31-35`](../backend/src/FinLedger.Infrastructure/Persistence/Configurations/AccountConfiguration.cs#L31-L35)
   usa o `xmin` do Postgres como número de versão. Isso é conceitualmente o mesmo que a técnica
   CAS (compare-and-swap) usada em estruturas de dados concorrentes lock-free: "só aplica minha
   mudança se a versão não mudou desde que eu li".

6. **Enum como conjunto fechado de valores** —
   [`AccountType.cs`](../backend/src/FinLedger.Domain/Enums/AccountType.cs) /
   [`TransactionType.cs`](../backend/src/FinLedger.Domain/Enums/TransactionType.cs) — um enum é,
   por baixo dos panos, um inteiro com nomes — a forma mais simples de "estrutura de dados" para
   representar um conjunto fixo e finito de opções.

7. **Log append-only (só INSERT, nunca UPDATE)** —
   [`Transaction.cs:5-7`](../backend/src/FinLedger.Domain/Entities/Transaction.cs#L5-L7) (comentário
   no código). Conceitualmente, isso é uma estrutura sequencial imutável (parecida com uma lista
   encadeada onde você só adiciona no fim) — é a mesma ideia por trás de "event sourcing" e do
   próprio Write-Ahead Log do Postgres.

### Exercícios propostos (para praticar o que o projeto não mostra)

O projeto te dá o domínio (contas, transações, saldo) — use-o como sandbox para implementar
estruturas de dados clássicas **na mão**, sem usar as prontas do .NET onde for pedido:

1. **Pilha (Stack)**: implemente "desfazer última transação" em memória, empilhando cada
   operação (`Deposit`, `Withdraw`) conforme ela acontece. Não use `System.Collections.Generic.Stack<T>`
   — implemente sua própria pilha com um array ou nó encadeado, e depois compare com a versão pronta.

2. **Fila (Queue)**: simule um "processamento em lote" de depósitos pendentes — várias
   transações entram numa fila e são processadas em ordem de chegada (FIFO).

3. **Lista encadeada + soma prefixada manual**: implemente sua própria lista encadeada de
   transações (nó com `Amount`, `Type`, `Next`) e escreva um método que percorre a lista
   calculando o saldo corrente linha a linha — reproduzindo **na mão**, em C#, o que a window
   function do PostgreSQL faz em [`StatementRepository.cs`](../backend/src/FinLedger.Infrastructure/Persistence/Repositories/StatementRepository.cs).
   Compare a complexidade: é O(n) nos dois casos, mas agora você entende o "por dentro".

4. **Busca binária**: dada uma lista de transações já ordenada por data (como a query já
   retorna), implemente busca binária para encontrar a primeira transação depois de uma data X,
   sem usar `List.BinarySearch`. Depois meça: por que isso só funciona porque a lista já está
   ordenada?

5. **Árvore de busca / `SortedDictionary`**: implemente um índice em memória de contas por
   saldo (para responder rápido "quais as 10 contas com maior saldo?") usando uma árvore
   balanceada. Compare com simplesmente fazer `.OrderByDescending(a => a.Balance).Take(10)` em uma
   lista — quando cada abordagem compensa?

6. **Tabela hash própria**: implemente uma tabela hash simples (array + função de hash + tratamento
   de colisão) para indexar contas por Id, e compare a complexidade de busca com `Dictionary<Guid,Account>`.

Cada exercício acima tem um "gabarito" natural: o próprio comportamento que o banco de dados já
garante no projeto real. Depois de implementar na mão, volte ao código do FinLedger e você vai
entender com muito mais profundeza *por que* cada padrão de banco documentado em
[`database-patterns.md`](database-patterns.md) existe.

---

## Roteiro de estudo sugerido

| Sessão | Foco | Arquivos |
|---|---|---|
| 1 | Domain puro — entidades, enums, exceções, interfaces | `Domain/Entities/*.cs`, `Domain/Enums/*.cs`, `Domain/Exceptions/*.cs`, `Domain/Interfaces/*.cs` |
| 2 | Casos de uso simples (CRUD) | `Application/Accounts/Commands/CreateAccount/*`, `Application/Accounts/Commands/CloseAccount/*` |
| 3 | Caso de uso complexo (o "chefão") | `Application/Transactions/Commands/Transfer/TransferCommandHandler.cs` — releia a Parte 1.4 antes |
| 4 | Persistência (EF Core + Dapper lado a lado) | `Infrastructure/Persistence/Repositories/*.cs`, `Infrastructure/Persistence/Configurations/*.cs` |
| 5 | Efeitos colaterais transversais | `Infrastructure/Persistence/Interceptors/AuditInterceptor.cs`, `Infrastructure/Persistence/UnitOfWork.cs` |
| 6 | HTTP e composição da aplicação | `API/Controllers/*.cs`, `API/Middleware/ExceptionHandlingMiddleware.cs`, `API/Program.cs` |
| 7 | Testes | `tests/FinLedger.Tests/Unit/AccountTests.cs`, `tests/FinLedger.Tests/Integration/TransferIntegrationTests.cs` |
| 8+ | Exercícios de estrutura de dados (Parte 3) | Fora do projeto — crie um novo console project separado para não poluir o FinLedger |

Depois desse roteiro, volte ao [`README.md`](../README.md) principal e releia a tabela de 13
padrões de banco — agora com o código já familiar, ela vai fazer muito mais sentido.
