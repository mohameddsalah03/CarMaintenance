# FIXORA Car Maintenance API
Welcome to the FIXORA Car Maintenance API — A RESTful API built with ASP.NET Core 8.0 for managing a car maintenance booking platform. The API handles customer bookings, technician auto-assignment, Paymob payment processing, AI-powered problem diagnosis, and real-time notifications via SignalR.

**Live API:** `http://carmaintenancefixora.runasp.net`

---

## Table of Contents
- [Project Overview](#project-overview)
- [Architecture](#architecture)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Features](#features)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [API Endpoints](#api-endpoints)
- [Authentication](#authentication)
- [Database Setup](#database-setup)
- [Services & Integrations](#services--integrations)
- [Error Handling](#error-handling)
- [Testing](#testing)

---

## Project Overview
FIXORA is a graduation project that connects car owners with certified maintenance technicians. Customers describe their problem, the AI suggests the right services, they pick a time slot, and the system auto-assigns the best available technician. After the job is done, they pay via cash or Paymob credit card.

### Key Capabilities:
- **Booking Management:** Full booking lifecycle from creation to payment with state-machine transitions
- **Technician Auto-Assignment:** Scoring algorithm based on specialization coverage, rating, and daily capacity
- **Available Slots:** Real-time slot calculation per technician across the next 7 days
- **AI Problem Diagnosis:** Arabic NLP microservice (MARBERT/AraBERT on Hugging Face) suggests relevant services from a text description
- **Payment Integration:** Paymob-powered credit card flow + cash option with HMAC-verified webhooks
- **Real-Time Notifications:** SignalR + DB-persisted notifications across all booking events
- **Vehicle Management:** Egyptian plate number validation and full CRUD
- **Admin Dashboard:** Booking stats, revenue, technician availability overview

---

## Architecture
The project follows Clean Architecture principles with the following layers:

### Layer Structure:

**CarMaintenance.APIs (Presentation Layer)**
- Controllers
- DTOs (Data Transfer Objects)
- Middleware
- Extension methods for service configuration

**CarMaintenance.Core.Domain (Domain Layer)**
- Business entities and enums
- Repository and unit of work interfaces
- Specification pattern contracts

**CarMaintenance.Core.Service (Application Services Layer)**
- Business logic implementations
- ServiceManager (lazy-initialized facade)
- AutoMapper profiles

**CarMaintenance.Infrastructure (Infrastructure Layer)**
- Paymob HTTP client
- AI Diagnosis HTTP client
- Background warm-up service (IHostedService)

**CarMaintenance.Infrastructure.Persistence (Data Layer)**
- EF Core DbContext and Fluent API configurations
- Generic repository and Unit of Work implementations
- Data seeding from JSON files

**CarMaintenance.Shared (Shared Layer)**
- All request/response DTOs
- Custom exceptions (NotFoundException, ForbiddenException, etc.)
- Settings classes bound from appsettings.json
- Utility helpers (plate number validation)

---

## Technology Stack

| Category | Technology |
|---|---|
| Framework | ASP.NET Core 8.0 |
| ORM | Entity Framework Core 8 |
| Database | SQL Server |
| Auth | ASP.NET Identity + JWT Bearer + Google OAuth |
| Real-Time | SignalR |
| Payments | Paymob (Credit Card + Cash) |
| AI Diagnosis Integration | HTTP client integration with an external Arabic NLP microservice |
| Object Mapping | AutoMapper 12 |
| Deployment | MonsterASP (IIS) |

---

## Project Structure

```
CarMaintenance/
├── CarMaintenance.APIs/                        # Controllers, middleware, extensions, DI registration
├── CarMaintenance.Core.Domain/                 # Entities, enums, specification contracts, IUnitOfWork
├── CarMaintenance.Core.Service/                # Business logic, service manager, AutoMapper profiles
├── CarMaintenance.Infrastructure/              # Paymob client, AI HTTP client, background warm-up worker
├── CarMaintenance.Infrastructure.Persistence/  # EF Core configs, repositories, data seeding
└── CarMaintenance.Shared/                      # DTOs, custom exceptions, settings, helpers
```

---

## Features

### 1. Authentication & Authorization
- **Register:** Create a new customer account with email, display name, phone number, and password
- **Login:** JWT-based authentication with short-lived access tokens (15 min) and refresh tokens
- **Google OAuth:** Sign in with Google — validates `id_token` via `GoogleJsonWebSignature`, creates the account on first login
- **Refresh Token:** Two lifetimes — 1 day (default) or 30 days (RememberMe). Rotation on every refresh
- **Forgot Password:** Sends a Base64Url-encoded reset link to the user's email via SMTP
- **Reset Password:** Validates the decoded token and updates the password through ASP.NET Identity
- **Account Lockout:** 5 failed login attempts triggers a 5-minute lockout

### 2. Vehicle Management
- Full CRUD for customer vehicles (Model, Brand, Year, PlateNumber)
- Egyptian plate validation: 1–3 Arabic letters + space + 1–4 digits
- Arabic-Indic digits (٠–٩) are normalized to ASCII automatically on save
- Plate uniqueness enforced at DB level (unique index)
- Vehicles with active bookings cannot be deleted

### 3. Service Catalog
- Paginated and filterable list of maintenance services (search, category, price range, max duration, sort)
- Service detail endpoint includes available technicians for that category
- Included/Excluded items and Requirements stored as JSON strings, deserialized on read
- Average rating and review count enriched per service from the Reviews table

### 4. Booking System
The core of the platform. A booking goes through the following states:

```
Pending → InProgress → WaitingClientApproval → InProgress → Completed
        ↘ Cancelled                           ↘ Cancelled
```

**Creating a Booking:**
1. Validates vehicle ownership and that no active booking exists on the same vehicle
2. Validates all service IDs and checks for duplicates
3. Persists the booking and booking-service records
4. Immediately attempts auto-assignment (never blocks creation on failure)
5. Sends booking confirmation + technician assignment notifications

**Technician Auto-Assignment Algorithm:**
1. Loads all available technicians
2. Normalizes service categories and technician specializations to lowercase English slugs
3. Computes coverage ratio (covered specs ÷ required specs)
4. Skips technicians below 50% coverage
5. Checks daily capacity — each technician is capped at 480 minutes (8h) per day
6. Full matches sorted by rating → partial matches sorted by coverage then rating
7. If no technician is available, notifies all admins with a manual-assignment prompt

**Additional Issues:**
- Technicians can flag extra work mid-booking (with cost and estimated duration)
- Critical issues are flagged separately — rejecting a critical issue resumes the booking with a report entry instead of blocking it
- Only one pending issue allowed at a time

**Available Slots Endpoint:**
- Loads active bookings per technician
- Walks each day (up to 7 days) in 60-min steps from 09:00–17:00
- Returns up to 6 slots total per technician, displayed in Cairo time (Egypt Standard Time)

### 5. AI Problem Diagnosis
```
POST /api/services/analyze-problem
{ "problemDescription": "فيه صوت غريب من المحرك", "vehicleId": 5 }
```
- Integrates with an external Arabic NLP microservice that analyzes the problem description and returns recommended service IDs
- Attaches vehicle context (brand, model, year) when the user is authenticated and provides a vehicleId
- Retries up to 3 times with exponential backoff on 5xx responses or timeouts
- A background `IHostedService` pings the microservice endpoint every 10 minutes to prevent cold-start delays
- Validates the recommended service IDs against the local database before returning them to the client

### 6. Payment Processing
Payment is intentionally deferred — initiated only after booking completion.

**Cash Flow:**
1. `POST /api/payments/initiate` with `paymentMethod: "Cash"`
2. Booking is immediately marked as Paid and a confirmation notification is sent

**Credit Card Flow (Paymob):**
1. `POST /api/payments/initiate` with `paymentMethod: "CreditCard"`
2. Backend: get auth token → create order → get payment key → build iFrame URL
3. Response contains the iFrame URL — frontend embeds it for card entry
4. Paymob calls `POST /api/payments/callback` with the transaction result
5. Backend verifies HMAC-SHA512 signature before processing
6. Idempotent — if the booking is already Paid, the callback is silently ignored

### 7. Real-Time Notifications
- Every significant booking event triggers a notification (creation, assignment, status change, payment, reviews)
- Notifications are saved to the database AND pushed over SignalR in the same call
- SignalR hub authenticates via JWT passed as the `access_token` query parameter (required for WebSocket connections)
- Customers can fetch paginated notification history, filter by read/unread, and mark individual or all as read
- Unread count is returned as both JSON body and `X-Unread-Count` response header

### 8. Reviews
- Customers can leave one review per completed booking (service rating + technician rating + comment)
- Technician's overall rating is recalculated as an average of all their TechnicianRating values on save
- Admins are notified when any rating is below 3/5
- Technicians can view all reviews they have received

### 9. Technician Management
- Admin creates a technician account — sends a set-password email link (valid 24h) instead of sending the raw password
- Specializations are stored as comma-separated English slugs (e.g., `oil_change,brakes,engine`)
- Availability can be toggled manually or is updated automatically on booking status changes
- Technicians with active bookings cannot be deleted

### 10. Admin Dashboard
- Real-time stats: total/pending/in-progress/completed/cancelled bookings, today's bookings
- Customer and technician counts (total + available)
- Total and today's revenue (from completed bookings)
- Average rating across all technicians
- Booking details with customer total booking count

---

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB or SQL Server Express)
- Visual Studio 2022 or VS Code

### Installation Steps

**1. Clone the Repository**
```bash
git clone https://github.com/mohameddsalah03/FIXORA-Backend.git
cd FIXORA-Backend
```

**2. Update Connection String**

Edit `CarMaintenance.APIs/appsettings.json` and update the connection string:
```json
{
  "ConnectionStrings": {
    "MainContext": "Server=.;Database=CarMaintenance.Main;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

**3. Restore NuGet Packages**
```bash
dotnet restore
```

**4. Run Database Migrations**

Migrations are automatically applied when the application starts. To apply manually:
```bash
dotnet ef database update --project CarMaintenance.Infrastructure.Persistence --startup-project CarMaintenance.APIs
```

**5. Run the Application**
```bash
cd CarMaintenance.APIs
dotnet run
```

**6. Access Swagger UI**

Navigate to: `https://localhost:{port}/swagger`

Swagger is enabled in both Development and Production environments.

---

## Configuration

### appsettings.json Structure
```json
{
  "ConnectionStrings": {
    "MainContext": "Your SQL Server connection string"
  },
  "JwtSettings": {
    "Key": "your-256-bit-secret-key",
    "Issuer": "CarIdentity",
    "Audience": "CarUsers",
    "DurationInMinutes": 15,
    "RefreshTokenShortDurationInDays": 1,
    "RefreshTokenDurationInDays": 30
  },
  "Authentication": {
    "Google": {
      "ClientId": "Your Google OAuth Client ID",
      "ClientSecret": "Your Google OAuth Client Secret"
    }
  },
  "EmailSettings": {
    "From": "your-email@gmail.com",
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "UserName": "your-email@gmail.com",
    "Password": "your-app-password"
  },
  "AppSettings": {
    "FrontendUrl": "http://localhost:5173"
  },
  "AISettings": {
    "DiagnosisUrl": "https://your-space.hf.space/api/analyze-problem",
    "ApiKey": "your-ai-api-key",
    "TimeoutSeconds": 120
  },
  "PaymobSettings": {
    "ApiKey": "Your Paymob API Key",
    "HmacSecret": "Your Paymob HMAC Secret",
    "CardIntegrationId": "Your Integration ID",
    "WalletIntegrationId": "Your Wallet Integration ID",
    "IFrameId": "Your iFrame ID"
  },
  "AllowedOrigins": [
    "http://localhost:5173",
    "http://localhost:3000"
  ]
}
```

### Important Configuration Notes:
- **JWT Key:** Must be at least 256 bits. Use a strong random string in production
- **Google OAuth:** Optional for local development — `google-login` endpoint will fail without it
- **Email (Gmail):** Use an App Password, not your account password. Enable 2FA first
- **AI Service:** Without the HuggingFace URL, `analyze-problem` returns a graceful fallback — it does not break other features
- **Paymob:** Use sandbox credentials for development. The Cash flow works independently of Paymob
- **CORS:** Add your frontend URL to `AllowedOrigins` — required for SignalR WebSocket connections

---

## API Endpoints

### Account

| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | `/api/account/register` | Public | Create a new customer account |
| POST | `/api/account/login` | Public | Login and receive tokens |
| POST | `/api/account/google-login` | Public | Sign in with Google id_token |
| POST | `/api/account/refresh-token` | Public | Rotate access + refresh tokens |
| GET | `/api/account/emailExists?email=` | Public | Check if email is already registered |
| POST | `/api/account/forgot-password` | Public | Send password reset email |
| POST | `/api/account/reset-password` | Public | Reset password with token from email |

**Register**
```
POST /api/account/register
Body:
{
  "displayName": "Mohamed Salah",
  "email": "user@example.com",
  "phoneNumber": "01012345678",
  "password": "Password@123"
}
Response: UserDto with access token and refresh token
```

**Login**
```
POST /api/account/login
Body:
{
  "email": "user@example.com",
  "password": "Password@123",
  "rememberMe": false
}
Response: UserDto with access token and refresh token
```

**Refresh Token**
```
POST /api/account/refresh-token
Body:
{
  "token": "expired-access-token",
  "refreshToken": "valid-refresh-token"
}
Response: UserDto with new access token and refresh token
```

---

### Vehicles

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/api/vehicles` | Auth | Get all vehicles for current user |
| GET | `/api/vehicles/{id}` | Auth | Get a specific vehicle |
| POST | `/api/vehicles` | Auth | Add a new vehicle |
| PUT | `/api/vehicles/{id}` | Auth | Update a vehicle |
| DELETE | `/api/vehicles/{id}` | Auth | Delete a vehicle (no active bookings) |
| GET | `/api/vehicles/all` | Admin | Get all vehicles in the system |

**Add Vehicle**
```
POST /api/vehicles
Headers: Authorization: Bearer {token}
Body:
{
  "model": "Corolla",
  "brand": "Toyota",
  "year": 2020,
  "plateNumber": "أ ب ج 1234"
}
Response: VehicleDto
```

---

### Services

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/api/services` | Public | Get paginated service catalog |
| GET | `/api/services/{id}` | Public | Get service by ID |
| GET | `/api/services/{id}/details` | Public | Get service with available technicians |
| POST | `/api/services` | Admin | Create a new service |
| PUT | `/api/services/{id}` | Admin | Update a service |
| DELETE | `/api/services/{id}` | Admin | Delete a service |
| POST | `/api/services/analyze-problem` | Public | AI-powered service recommendation |

**Get Services**
```
GET /api/services?category=oil_change&minPrice=50&maxPrice=500&sort=priceAsc&pageIndex=1&pageSize=9
Response: Pagination<ServiceDto>
```

**Analyze Problem (AI)**
```
POST /api/services/analyze-problem
Body:
{
  "problemDescription": "فيه صوت غريب من المحرك وعدم توازن في العجل",
  "vehicleId": 5
}
Response:
{
  "status": "diagnosed",
  "suggestedServices": [
    {
      "serviceId": 3,
      "serviceName": "Engine Inspection",
      "category": "engine",
      "basePrice": 250.00,
      "estimatedDurationMinutes": 60,
      "confidence": 0.91
    }
  ],
  "message": "تم التشخيص بنجاح"
}
```

---

### Bookings

| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | `/api/bookings` | Customer | Create a new booking |
| GET | `/api/bookings/my-bookings` | Customer | Get paginated bookings for current user |
| GET | `/api/bookings/{id}` | Customer | Get booking details |
| PATCH | `/api/bookings/{id}/cancel` | Customer | Cancel a booking |
| GET | `/api/bookings/{id}/invoice` | Customer | Get booking invoice |
| PATCH | `/api/bookings/additional-issues/{issueId}/approve` | Customer | Approve or reject an additional issue |
| GET | `/api/bookings/available-slots` | Customer | Get available time slots per technician |
| GET | `/api/bookings/all` | Admin | Get all bookings with filters |
| GET | `/api/bookings/today` | Admin, Technician | Get today's bookings |
| POST | `/api/bookings/{id}/assign-technician` | Admin | Manually assign a technician |
| GET | `/api/bookings/my-assignments` | Technician | Get assigned bookings |
| GET | `/api/bookings/{id}/details` | Technician | Get booking details as technician |
| PATCH | `/api/bookings/{id}/update-status` | Technician | Update booking status (InProgress / Completed) |
| POST | `/api/bookings/{id}/additional-issues` | Technician | Report an additional issue |

**Create Booking**
```
POST /api/bookings
Headers: Authorization: Bearer {token}
Body:
{
  "vehicleId": 5,
  "scheduledDate": "2026-07-10T10:00:00Z",
  "description": "تغيير زيت + فحص المكابح",
  "services": [
    { "serviceId": 1, "duration": 60 },
    { "serviceId": 4, "duration": 45 }
  ]
}
Response: BookingDto with assigned technician (if auto-assignment succeeded)
```

**Get Available Slots**
```
GET /api/bookings/available-slots?serviceIds=1&serviceIds=4
Headers: Authorization: Bearer {token}
Response:
{
  "serviceIds": [1, 4],
  "totalDurationMinutes": 105,
  "technicians": [
    {
      "technicianId": "tech-uuid",
      "technicianName": "Ahmed Ali",
      "specialization": "oil_change,brakes",
      "rating": 4.8,
      "isFullMatch": true,
      "availableSlots": [
        { "slotDateTime": "2026-07-08T07:00:00Z", "label": "غداً 9:00 ص" }
      ]
    }
  ]
}
```

**Update Booking Status (Technician)**
```
PATCH /api/bookings/{id}/update-status
Headers: Authorization: Bearer {token}
Body:
{
  "status": "InProgress",
  "technicianReport": "بدأت في الفحص"
}
Response: BookingDto
```

---

### Payments

| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | `/api/payments/initiate` | Customer | Start payment (Cash or CreditCard) |
| POST | `/api/payments/callback` | Public | Paymob webhook receiver |

**Initiate Payment**
```
POST /api/payments/initiate
Headers: Authorization: Bearer {token}
Body:
{
  "bookingId": 12,
  "paymentMethod": "CreditCard"
}
Response:
{
  "iFrameUrl": "https://accept.paymob.com/api/acceptance/iframes/...",
  "paymentToken": "..."
}
```

---

### Notifications

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/api/notifications` | Auth | Get paginated notifications |
| GET | `/api/notifications/unread-count` | Auth | Get unread count |
| PATCH | `/api/notifications/{id}/mark-read` | Auth | Mark one as read |
| PATCH | `/api/notifications/mark-all-read` | Auth | Mark all as read |

**Get Notifications**
```
GET /api/notifications?pageIndex=1&pageSize=10&isRead=false
Headers: Authorization: Bearer {token}
Response: Pagination<NotificationDto>
Response Headers: X-Unread-Count: 5
```

---

### Reviews

| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | `/api/reviews/{bookingId}` | Customer | Submit a review for a completed booking |
| GET | `/api/reviews/{bookingId}` | Customer | Get review for a specific booking |
| GET | `/api/reviews/all` | Admin | Get all reviews |
| GET | `/api/reviews/my-reviews` | Technician | Get all reviews received |

**Create Review**
```
POST /api/reviews/12
Headers: Authorization: Bearer {token}
Body:
{
  "serviceRating": 5,
  "technicianRating": 4,
  "comment": "خدمة ممتازة والفني محترف"
}
Response: ReviewDto
```

---

### Technicians

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/api/technicians` | Public | Get all technicians |
| GET | `/api/technicians/available` | Public | Get available technicians |
| GET | `/api/technicians/{id}` | Public | Get technician by ID |
| POST | `/api/technicians` | Admin | Create a technician account |
| PUT | `/api/technicians/{id}` | Admin, Technician | Update technician info |
| DELETE | `/api/technicians/{id}` | Admin | Delete technician (no active bookings) |
| PATCH | `/api/technicians/{id}/toggle-availability` | Admin, Technician | Toggle availability status |

**Create Technician**
```
POST /api/technicians
Headers: Authorization: Bearer {token} (Admin)
Body:
{
  "displayName": "Khaled Hassan",
  "userName": "khaled_tech",
  "email": "khaled@fixora.com",
  "phoneNumber": "01098765432",
  "password": "Temp@1234",
  "specialization": "oil_change,brakes,engine",
  "experienceYears": 5
}
Response: TechniciansDto (set-password link sent to email)
```

---

### Admin

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/api/admin/dashboard-stats` | Admin | Full dashboard statistics |
| GET | `/api/admin/bookings/{id}` | Admin | Booking details with customer history |
| GET | `/api/admin/customers` | Admin | Get all customers |

---

## Authentication

The API uses JWT (JSON Web Tokens) for authentication.

### How It Works:
1. **Register or Login:** User provides credentials
2. **Token Generation:** Server generates an access token (15 min) and a refresh token
3. **Token Response:** Both tokens returned to client along with expiry time
4. **Subsequent Requests:** Client includes access token in the Authorization header:
   ```
   Authorization: Bearer {your_access_token}
   ```
5. **Token Refresh:** Before expiry, client calls `/api/account/refresh-token` with both tokens to get a new pair
6. **SignalR:** Pass the token as a query parameter — `?access_token={token}` — because WebSocket connections cannot set custom headers

### Token Structure:
- **Algorithm:** HMAC SHA-256
- **Claims:** NameIdentifier (userId), Name (userName), Email, Role(s)
- **Access Token Expiry:** 15 minutes (configurable)
- **Refresh Token Expiry:** 1 day (default) / 30 days (RememberMe)

### Protected Endpoints:
All endpoints require authentication except:
- All product/service listing
- User registration and login
- Google login
- Email existence check
- Paymob callback webhook

---

## Database Setup

### Database Context
The project uses a single `CarDbContext` that extends `IdentityDbContext<ApplicationUser>`.

Identity tables are mapped to custom names:

| Identity Table | Custom Name |
|---|---|
| AspNetUsers | Users |
| AspNetRoles | Roles |
| AspNetUserRoles | UserRoles |

All other Identity tables (claims, tokens, logins) are ignored since they are not needed.

### Application Tables

| Table | Description |
|---|---|
| Users | Customer and technician accounts |
| Technicians | Technician profiles linked to Users |
| Vehicles | Customer vehicles |
| Services | Maintenance service catalog |
| Bookings | Booking records |
| BookingServices | Many-to-many: bookings ↔ services |
| AdditionalIssues | Extra work flagged mid-booking |
| Reviews | Customer reviews per booking |
| Notifications | Per-user notification records |

### Migrations
Migrations are automatically applied on application startup. To create a new migration manually:
```bash
dotnet ef migrations add MigrationName \
  --project CarMaintenance.Infrastructure.Persistence \
  --startup-project CarMaintenance.APIs \
  --context CarDbContext
```

### Data Seeding
On every startup, the seeder checks each table before inserting. Tables with data are skipped entirely (idempotent).

Tables that use `IDENTITY_INSERT ON` during seeding (to preserve JSON IDs):
- Vehicles, Services, Bookings, BookingServices

Seeding order (dependency-aware):
1. Roles → Users → Technicians → Vehicles
2. Services → Bookings → BookingServices
3. AdditionalIssues → Notifications → Reviews

---

## Services & Integrations

### 1. JWT Token Service
- **Purpose:** Generate signed access tokens and stateless refresh tokens
- **Implementation:** `AuthService` using `Microsoft.IdentityModel.Tokens`
- **Refresh Token:** Random 64-byte value stored in the `Users` table with expiry time

### 2. Email Service
- **Purpose:** Send password reset links and technician onboarding emails
- **Implementation:** `EmailService` using `System.Net.Mail.SmtpClient`
- **Config:** Gmail SMTP on port 587 with TLS. Requires a Gmail App Password

### 3. AI Diagnosis Service
- **Purpose:** Recommend maintenance services from an Arabic problem description
- **Implementation:** `AiDiagnosisService` using a typed `IHttpClientFactory` client to call an external NLP microservice
- **Retry Policy:** Up to 3 attempts, 2s × attempt delay on 5xx or timeout
- **Warm-Up:** `AiWarmUpService` (IHostedService) sends a dummy request every 10 minutes to prevent cold-start delays on the microservice host

### 4. Paymob Service
- **Purpose:** Credit card payment processing via Paymob's Accept gateway
- **Flow:** `GetAuthTokenAsync` → `CreateOrderAsync` → `GetPaymentKeyAsync` → `BuildIFrameUrl`
- **Webhook Security:** `VerifyHmac` computes HMAC-SHA512 over the transaction fields in Paymob's required concatenation order
- **Implementation:** `PaymobService` using `IHttpClientFactory`

### 5. Notification Service
- **Purpose:** Persist notifications to DB and deliver them in real-time
- **Implementation:** `NotificationService` calling `IHubContext<NotificationHub>`
- **Targeting:** `Clients.User(userId)` — uses the `NameIdentifier` claim as the user identifier

### 6. Unit of Work
- **Purpose:** Coordinate repository access and manage `SaveChangesAsync` calls
- **Implementation:** `UnitOfWork` with a `Dictionary<string, object>` repo cache — one repository instance per entity type per request scope

---

## Error Handling

### Error Response Types:

**Standard Error:**
```json
{
  "statusCode": 404,
  "errorMessage": "Booking with ID '99' was not found."
}
```

**Validation Error:**
```json
{
  "statusCode": 400,
  "errorMessage": "One or more validation errors occurred.",
  "errors": ["Phone number must start with 010, 011, 012, or 015"]
}
```

**Unhandled Exception (Development):**
```json
{
  "statusCode": 500,
  "errorMessage": "Connection timeout"
}
```

### Exception-to-Status Code Mapping:

| Exception | HTTP Status |
|---|---|
| `NotFoundException` | 404 Not Found |
| `UnauthorizedException` | 401 Unauthorized |
| `ForbiddenException` | 403 Forbidden |
| `ValidationException` | 400 Bad Request |
| `BadRequestException` | 400 Bad Request |
| Unhandled Exception | 500 Internal Server Error |

The global `ExceptionHandlerMiddleware` catches all unhandled exceptions and maps them to the correct status codes and response shapes. It also handles `404 Not Found` for unmapped routes.

---

## Testing

### Using Swagger UI
1. Navigate to `/swagger`
2. Click **Authorize** and enter: `Bearer {your_token}`
3. Register/Login first to get a token
4. Test endpoints interactively

### Using Postman
1. Create a collection for FIXORA
2. Add a `POST /api/account/login` request and save the token to a collection variable
3. Set `Authorization: Bearer {{token}}` at the collection level
4. Test endpoints in dependency order: Register → Add Vehicle → Get Services → Create Booking → Initiate Payment

### SignalR Testing
To test real-time notifications:
1. Connect to `wss://localhost:{port}/hubs/notifications?access_token={token}`
2. Listen on the `ReceiveNotification` event
3. Trigger a booking action (create, update status, etc.) from another client
