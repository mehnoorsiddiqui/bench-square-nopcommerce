# square-plan.md — Phase 1: swap `Square 36.0.0` for `ApimaticBench.Square 1.0.0`

Task: replace Square's own SDK (`Square` 36.0.0, Square API 2024-04-17) in `src/Plugins/Nop.Plugin.Payments.Square`
with the APIMatic Codegen v4 SDK `ApimaticBench.Square` 1.0.0 (generated from the same Square API version, S0),
keeping the plugin's behaviour: same 9 operations, same fields sent and read, same error messages to nopCommerce.
Plan written by the main agent (the `square-planner` agent is not installed in this host). Grounded in the SDK map
at `v1.0.0` (`sdk-map.md`, `map/operations/`), and the model / request files those pages name.

## 1. Scope & sequence

| # | Step | Operations | Files |
| --- | --- | --- | --- |
| 1 | Package: remove `Square` 36.0.0, add `ApimaticBench.Square` 1.0.0 | — | `Nop.Plugin.Payments.Square.csproj` |
| 2 | Client: one named `HttpClient` from `IHttpClientFactory`; `SquareClient` per call with that store's settings | — | `Infrastructure/NopStartup.cs`, `Services/SquarePaymentManager.cs`, `SquarePaymentDefaults.cs` |
| 3 | Calls: request records instead of positional args | all 9 | `Services/SquarePaymentManager.cs` |
| 4 | Errors: `ApiException<RawError>` instead of `ApiException.Errors` | all 9 | `Services/SquarePaymentManager.cs` |
| 5 | Request bodies: object initializers instead of builders/constructors; `integration_id` through `AdditionalProperties` | CreatePayment, CreateCustomer, CreateCard, CompletePayment, RefundPayment | `SquarePaymentMethod.cs`, `Extensions/CreatePaymentRequestExtensions.cs`, `Domain/ExtendedCreatePaymentRequest.cs` (deleted) |
| 6 | Build inside nopCommerce; no SDK error left | — | — |

Unchanged readers (same member names at 1.0.0): `Components/PaymentSquareViewComponent.cs` (`Customer.Cards`, `Card.Last4`, `Card.Id`), `Controllers/PaymentSquareController.cs` (`Location.BusinessName`, `.Name`, `.Id`).

## 2. CONTRACT SHEET

> ⚠ Signatures are generated code, verbatim: every operation that takes input takes ONE request record as its first parameter, built with an object initializer using the record's own property names — never flat arguments. ListLocations takes none.
> ⚠ Every SDK type is written fully qualified from the namespace its source path implies: `Square.Requests.<Controller>` for request records, `Square.Models` for models, `Square.Core.Exceptions` / `Square.Core.ErrorResponse` / `Square.Core.Models` / `Square.Core.Hooks` / `Square.Servers` for the rest.

All 9: **Auth** `options.BearerAuth`; **Error** `ApiException<RawError>` — Case B (`StatusCode`, `ReadAsBytes`, `ReadAsString`, `ReadAsJson<T>`); no pagination; server group `Default`. Every response model also carries `Errors: IReadOnlyList<Error>?` (`errors`).

| Op | Call | Request record (required) | Body model fields used (`wire`) | Response → field read | Source |
| --- | --- | --- | --- | --- | --- |
| CreatePayment | `client.Payments.CreatePayment(CreatePaymentOperationRequest, RequestOptions?, CancellationToken)` | `Square.Requests.Payments.CreatePaymentOperationRequest { Body: CreatePaymentRequest (req) }` | `SourceId (source_id) string req`, `IdempotencyKey (idempotency_key) string req`, `AmountMoney (amount_money) Money?`, `Autocomplete (autocomplete) bool?`, `CustomerId (customer_id)`, `LocationId (location_id)`, `ReferenceId (reference_id)`, `VerificationToken (verification_token)`, `BuyerEmailAddress (buyer_email_address)`, `BillingAddress (billing_address) Address?`, `ShippingAddress (shipping_address) Address?`, `Note (note)` (all optional `string?` unless noted); `AdditionalProperties` bag (`Square.Core.Models.AdditionalProperties`) | `CreatePaymentResponse.Payment: Payment?` → `Id: string?`, `Status: string?` | map/operations/Payments.md; Models/CreatePaymentRequest.cs, CreatePaymentResponse.cs, Payment.cs |
| CompletePayment | `client.Payments.CompletePayment(CompletePaymentOperationRequest, …)` | `{ PaymentId: string (req), Body: CompletePaymentRequest (req) }` | `CompletePaymentRequest`: `VersionToken (version_token) string?` (not set) | `CompletePaymentResponse.Errors` only | Payments.md; Requests/Payments/CompletePaymentOperationRequest.cs |
| CancelPayment | `client.Payments.CancelPayment(CancelPaymentRequest, …)` | `Square.Requests.Payments.CancelPaymentRequest { PaymentId: string (req) }` | — | `CancelPaymentResponse.Errors` only | Payments.md; Requests/Payments/CancelPaymentRequest.cs |
| RefundPayment | `client.Refunds.RefundPayment(RefundPaymentOperationRequest, …)` | `{ Body: RefundPaymentRequest (req) }` | `IdempotencyKey (idempotency_key) string req`, `AmountMoney (amount_money) Money req`, `PaymentId (payment_id) string?` | `RefundPaymentResponse.Refund: PaymentRefund?` → `Status: string?` | Refunds.md; Models/RefundPaymentRequest.cs, PaymentRefund.cs |
| CreateCustomer | `client.Customers.CreateCustomer(CreateCustomerOperationRequest, …)` | `{ Body: CreateCustomerRequest (req) }` | `EmailAddress (email_address)`, `Nickname (nickname)`, `GivenName (given_name)`, `FamilyName (family_name)`, `PhoneNumber (phone_number)`, `CompanyName (company_name)`, `ReferenceId (reference_id)` — all `string?`; no member required | `CreateCustomerResponse.Customer: Customer?` → `Id` | Customers.md; Models/CreateCustomerRequest.cs |
| RetrieveCustomer | `client.Customers.RetrieveCustomer(RetrieveCustomerRequest, …)` | `Square.Requests.Customers.RetrieveCustomerRequest { CustomerId: string (req) }` | — | `RetrieveCustomerResponse.Customer` → `Id`, `Cards: IReadOnlyList<Card>?` | Customers.md; Models/Customer.cs |
| CreateCard | `client.Cards.CreateCard(CreateCardOperationRequest, …)` | `{ Body: CreateCardRequest (req) }` | `IdempotencyKey (idempotency_key) req`, `SourceId (source_id) req`, `Card (card) Card req`, `VerificationToken (verification_token) string?`; `Card`: `CustomerId (customer_id)`, `BillingAddress (billing_address) Address?` | `CreateCardResponse.Card: Card?` → `Id`, `Last4 (last_4)` | Cards.md; Models/CreateCardRequest.cs, Card.cs |
| ListLocations | `client.Locations.ListLocations(RequestOptions?, CancellationToken)` | none | — | `ListLocationsResponse.Locations: IReadOnlyList<Location>?` → `Id`, `Name`, `BusinessName`, `Status: string?`, `Capabilities: IReadOnlyList<string>?` | Locations.md; Models/Location.cs |
| RetrieveLocation | `client.Locations.RetrieveLocation(RetrieveLocationRequest, …)` | `Square.Requests.Locations.RetrieveLocationRequest { LocationId: string (req) }` | — | `RetrieveLocationResponse.Location: Location?` (same fields) | Locations.md |

Shared models: `Money { Amount (amount): long?, Currency (currency): string? }`; `Address { AddressLine1 (address_line_1), AddressLine2 (address_line_2), AdministrativeDistrictLevel1, AdministrativeDistrictLevel2, Locality, PostalCode, Country, FirstName, LastName }` — all `string?`; `Error { Category (category) string req, Code (code) string req, Detail (detail) string?, Field (field) string? }`. All records, `init`-only; copy-with-change is `with { … }`. (Models/Money.cs, Address.cs, Error.cs)

Enums needed: none — at 1.0.0 `Location.Status`, `Location.Capabilities`, `Payment.Status`, `PaymentRefund.Status`, `Money.Currency`, `Address.Country` are plain `string`s, so the plugin's `SquarePaymentDefaults.Status` string constants still compare.

Client construction / auth / servers (sdk-map.md *Getting a client*, *Servers & auth*):
- `new Square.SquareClient(HttpClient httpClient, Square.SquareClientOptions options)` — the only constructor.
- `options.BearerAuth = settings.AccessToken` (replaces `BearerAuthModel`).
- `options.Environment = Square.Servers.ServerEnvironment.Environment2` (Sandbox, `https://connect.squareupsandbox.com`) or `.Production` (`https://connect.squareup.com`); one server group `Default`.
- `options.Hooks`: `Square.Core.Hooks.SdkHook.OnRequest(Action<HttpRequestMessage, HookContext>)` — replaces `AddAdditionalHeader("user-agent", …)`.
- `options.Retry` (`Square.Core.Configuration.RetryOptions`, change only via `RetryOptions.Default() with { … }`), `options.Logging` (`LoggingOptions { LoggerFactory }`).

### CROSS-OPERATION INVARIANTS

| Invariant | Operations | Enforced where |
| --- | --- | --- |
| The location a payment is taken at must be an active, card-processing location of the account | `CreatePayment` ← `RetrieveLocation` (configured `LocationId`) | `SquarePaymentMethod.CreatePaymentRequestAsync` → `SquarePaymentManager.GetSelectedActiveLocationAsync` (unchanged check on `Status` / `Capabilities`) before `CreatePayment` |
| A stored card used as `SourceId` must belong to the Square customer saved for this nop customer | `CreatePayment` ← `RetrieveCustomer` / `CreateCard` | `CreatePaymentRequestAsync` (unchanged): retrieves the customer by the saved `CustomerIdAttribute` before using the card on file |
| Capture / void / refund act on the payment id this store's earlier `CreatePayment` returned | `CompletePayment`, `CancelPayment`, `RefundPayment` ← `CreatePayment` | `CaptureAsync` / `VoidAsync` / `RefundAsync` read `Order.AuthorizationTransactionId` / `CaptureTransactionId` saved from `Payment.Id` (unchanged) |

## 3. Trap notes

- Step 2: what a per-call `SquareClient` costs, and where the `HttpClient` it wraps must come from — MUST load `bench-square:dotnet-client-initialization`.
- Step 2: bearer credential binding and what a blank token does at call time — MUST load `bench-square:dotnet-authentication`.
- Step 2: what `Retry.Timeout` and `HttpClient.Timeout` each bound, and which verbs the SDK re-sends — MUST load `bench-square:dotnet-configuration-resilience`.
- Step 2: a hand-built client with no `LoggerFactory` can be switched to unredacted body logging (card nonces, addresses) without a code change — MUST load `bench-square:dotnet-configuration-resilience`.
- Step 3: request-record vs body-model shape, and `RequestOptions` — MUST load `bench-square:dotnet-calling-endpoints`.
- Step 4: Case B body may not be JSON; what escapes a catch that only names `ApiException<RawError>` — MUST load `bench-square:dotnet-error-handling`.
- Step 5: `required` members, records and `with`, and how an undeclared wire field (`integration_id`) is sent and whether a `with` copy shares it — MUST load `bench-square:dotnet-models`.

## 4. REQUIRED READING

- `bench-square:dotnet-getting-started` — identity, namespaces, map use (all steps)
- `bench-square:dotnet-client-initialization` — step 2
- `bench-square:dotnet-authentication` — step 2
- `bench-square:dotnet-configuration-resilience` — step 2 (timeouts, retries, logging, hooks)
- `bench-square:dotnet-calling-endpoints` — step 3
- `bench-square:dotnet-models` — step 5
- `bench-square:dotnet-error-handling` — step 4
- Hazard (verbatim): a body that does not match its declared type — a drifted or malformed **2xx** response (a missing `required` member) or a **non-2xx** body that does not match its operation's generated `{Operation}Error` shape — surfaces as `ResponseDeserializationException`, an `ApiException` that keeps the HTTP status and names the target type but is **not** an `ApiException<TError>`; a catch ladder that handles only `ApiException<TError>` lets it escape, so it must also catch `ResponseDeserializationException` (or `ApiException`).

## 5. PRODUCTION READINESS

| # | Concern | Decision |
| --- | --- | --- |
| 1 | Credential fail-fast | Unchanged from the plugin: the token is a per-store nop setting read at call time; `CreateSquareClientAsync` throws `NopException` when Sandbox is on and `AccessToken` is blank. A blank production token now fails with `AuthSchemeException` before sending, caught by the manager's catch-all and logged. No host-start check (settings are per store and editable at runtime). |
| 2 | Secret sourcing & rotation | `SquarePaymentSettings.AccessToken` (nop settings, per store), read on every call: a rotated token applies on the next call; no DI singleton captures it. |
| 3 | Total timeout budget | Named `HttpClient` `Timeout` = 30 s and `options.Retry` `Timeout` = 30 s per attempt (`SquarePaymentDefaults.RequestTimeout`). Writes (`POST`) get one attempt: ≤ 30 s. Reads (`GET`) are retried by the SDK: ≤ 4 × 30 s + 7 s ≈ 127 s worst case; `HttpClient.Timeout` ends a hang at ≈ 30 s on the first attempt. |
| 4 | Write-retry ownership | SDK defaults kept: `POST` (CreatePayment, CompletePayment, CancelPayment, RefundPayment, CreateCustomer, CreateCard) is never re-sent; `GET` (ListLocations, RetrieveLocation, RetrieveCustomer) is retried on 408/429/5xx/transport/timeouts. The plugin's own retry of a `PENDING` refund re-sends the same `RefundPaymentRequest` (same `IdempotencyKey`) — unchanged. |
| 5 | Idempotency & ambiguous writes | Real keys are body members: `CreatePaymentRequest.IdempotencyKey`, `RefundPaymentRequest.IdempotencyKey`, `CreateCardRequest.IdempotencyKey` (one `Guid` per attempt, as before); `CreatePaymentRequest.ReferenceId` = order GUID. CompletePayment / CancelPayment act on a payment id (state transition). CreateCustomer: no key (`ReferenceId` = nop customer GUID). The injected `Idempotency-Key` header is not a key. |
| 6 | Observability | Unchanged: every failure is logged once through nop `ILogger.ErrorAsync` with the exception and, for `ApiException<RawError>`, the joined `errors[].detail`, plus HTTP status. No provider correlation id is exposed by Case B bodies (`UNVERIFIED`). |
| 7 | Sensitive data | Yes — card nonce (`source_id`), verification token, billing/shipping address, email. `options.Logging.LoggerFactory = NullLoggerFactory.Instance` set explicitly (disables `SQUARE_LOG`); `LogRequestBody` stays off; nothing echoes a request body. |
| 8 | Environment selection | One server group `Default`. `UseSandbox` → `ServerEnvironment.Environment2` (`connect.squareupsandbox.com`), else `ServerEnvironment.Production`. |
| 9 | Duplicate prevention under concurrency | See DUPLICATE CLAIMS. |
| 10 | Partial results | See PAGED READS (`none`). |
| 11 | Unknown outcomes | See UNKNOWN OUTCOMES. |

**DUPLICATE CLAIMS**

| Write | Where the claim is stored | What rejects the second one | Where that rejection is caught | Where in the code |
| --- | --- | --- | --- | --- |
| CreatePayment (checkout) | nopCommerce `Order` row — the order is created by nop's checkout before payment processing, keyed by `OrderGuid` | nop's order placement for the same cart/`OrderGuid` (not changed by this swap) | nopCommerce `OrderProcessingService` | `SquarePaymentMethod.ProcessPaymentAsync` (`CreatePaymentRequestAsync`) → `SquarePaymentManager.CreatePaymentAsync` (`client.Payments.CreatePayment`) |

**PAGED READS**: none — `ListLocations` has no pagination at 1.0.0 (Locations.md: no Pagination bullet).

**UNKNOWN OUTCOMES**

| Write | The operation you re-read with | The reference you search by | Where in the code | The test that fails the connection |
| --- | --- | --- | --- | --- |
| CreatePayment | none called by this plugin today | `CreatePaymentRequest.ReferenceId` = order GUID | Unchanged: `SdkConnectionException` lands in `CreatePaymentAsync`'s catch, is logged and reported as a payment error (see §6) | none (the plugin has no tests; Phase 3 adds them) |
| CompletePayment / CancelPayment / RefundPayment | none today | payment id / refund idempotency key | Unchanged: reported as an error to the admin, who can retry | none |

## 6. Assumptions & Blockers

- **Behaviour-preserving swap (assumption).** Phase 1 is the SDK swap that freezes the benchmark baseline; it must not add features. So the original plugin's handling of unknown write outcomes (a transport failure on CreatePayment is reported as a failed payment, not re-read) and of duplicate submissions (left to nopCommerce's order placement) is kept as it was, not redesigned. Changing it would make the baseline differ from the plugin under test. Recorded, not a blocker.
- **Package version pinned (decision).** The getting-started skill installs version-less; the benchmark needs an exact baseline, so the reference is `ApimaticBench.Square` `1.0.0`.
- **`integration_id`.** The old plugin subclassed the request to add `integration_id`; `CreatePaymentRequest` 1.0.0 does not declare it, so it goes through `AdditionalProperties.Set("integration_id", …)` (wire name, models skill *Unknown / future fields*). Whether Square still accepts it is `UNVERIFIED` (only live traffic).
- **Error body shape.** Square's error responses are assumed to carry the same `errors: Error[]` envelope the success models declare (`errors` on every `*Response`) — `UNVERIFIED`; the code falls back to `ReadAsString()` when the body is not that JSON.
- **Sandbox environment name.** v4 names the Sandbox server `Environment2` (generator naming, not Square's); used as-is.
- No blockers.
