# EShoppingZone - E-Commerce Platform 

**Version:** 1.0 | **Year:** 2026 | **Framework:** .NET 8 / ASP.NET Core / EF Core / PostgreSQL

EShoppingZone is a full-stack E-Commerce platform built with .NET 8 and ASP.NET Core. The platform is designed as a set of independently deployable microservices, orchestrated through an API Gateway. It supports multiple roles including Customers, Merchants, and Admins, and provides core e-commerce functionalities such as product browsing, cart management, order processing, and e-wallet payments.

---

## 📅 UC-1: Project Setup & API Gateway Initialization

### 🎯 Goal
Set up the base microservices and configure API Gateway routing to ensure seamless inter-service communication.

### 📌 Description
This use case covers the initial project setup, including:

- Creation of solution and microservices:
  - **Profile-Service**
  - **Product-Service**
  - **Cart-Service**
  - **Order-Service**
  - **Wallet-Service**
- Establishment of Clean Architecture structure in each microservice.
- Configuration of API Gateway using **YARP** to route requests to the appropriate services.
- Integration with **PostgreSQL** (via Supabase) for database connectivity.
- Implementation of basic **health check endpoints** (`/health`) in each service.

### 🛠️ Tasks
1. **Solution & Service Setup**
   - Initialize the .NET solution.
   - Create individual microservices: Profile, Product, Cart, Order, Wallet.

2. **Clean Architecture**
   - Organize each service into layers:
     - Domain
     - Application/Service
     - Infrastructure
     - API/Controller
     
3. **API Gateway Configuration**
   - Install YARP (Yet Another Reverse Proxy) NuGet package.
   - Configure routing to all microservices.
   - Enable request forwarding and basic route validation.

4. **Database Connection**
   - Set up PostgreSQL connection for each service using Supabase.
   - Configure EF Core DbContext in each microservice.

5. **Health Check Endpoints**
   - Implement `/health` endpoint in each microservice to report service status.
   - Use `Microsoft.Extensions.Diagnostics.HealthChecks`.

---

### 🧪 Postman Testing

**Objective:** Verify that all services are reachable through the API Gateway.

**Test Cases:**

| Request | Expected Response |
|---------|-----------------|
| `GET /health` via Gateway | `200 OK` with service status JSON for each microservice |

**Example Response:**
```json
{
  "status": "Healthy",
  "service": "Profile-Service"
}
```
---

## 📅 UC-2: Authentication & User Registration/Login

### 🎯 Goal
Implement a secure authentication system using JWT with support for role-based access control and OAuth readiness (Google login).

---

### 📌 Description
This use case focuses on building a complete authentication and authorization system for the EShoppingZone platform.

It includes:

- User registration and login functionality.
- Secure password storage using hashing.
- JWT (JSON Web Token) generation for stateless authentication.
- Role-based access control (RBAC) for:
  - **Customer**
  - **Merchant**
  - **Admin**
- Integration readiness for third-party authentication (Google OAuth).
- Protection of APIs using JWT-based authorization middleware.

---

### 🛠️ Tasks

#### 1. **User Entity Design**
- Create a `User` entity in the **Domain Layer** with fields:
  - `Id (GUID)`
  - `FullName`
  - `Email`
  - `PasswordHash`
  - `Role` (Enum: Customer, Merchant, Admin)
  - `CreatedAt`
- Apply validation rules (Email format, password length, etc.)

---

#### 2. **Register API**
- Endpoint: `POST /auth/register`
- Validate incoming request:
  - Email uniqueness
  - Strong password rules
- Hash password using **BCrypt**
- Store user in database
- Return success response with basic user info

---

#### 3. **Login API**
- Endpoint: `POST /auth/login`
- Validate credentials:
  - Check email exists
  - Verify password hash
- Generate JWT token on successful authentication
- Return token and user details

---

#### 4. **JWT Token Generation**
- Configure JWT Authentication:
  - Secret Key
  - Issuer & Audience
  - Expiry (e.g., 1 hour)
- Include claims in token:
  - `UserId`
  - `Email`
  - `Role`
- Configure middleware:
  ```csharp
  app.UseAuthentication();
  app.UseAuthorization();

---

## 📅 UC-3: Profile Management

### 🎯 Goal
Enable users to manage their profile information and addresses (CRUD operations).

---

### 📌 Description
This use case focuses on providing functionality for users to view and update their personal details, as well as manage multiple addresses.

It includes:

- Fetching user profile details.
- Updating profile information.
- Adding and managing multiple addresses.
- Securing all endpoints using JWT authentication.

---

### 🛠️ Tasks

#### 1. **Get User Profile**
- Endpoint: `GET /profile/{id}`
- Fetch user details from database.
- Return:
  - Full Name
  - Email
  - Role
  - Created Date

---

#### 2. **Update Profile**
- Endpoint: `PUT /profile/update`
- Allow user to update:
  - Full Name
  - Email (optional with validation)
- Validate input fields.
- Save updated data to database.

---

#### 3. **Add Address**
- Endpoint: `POST /profile/address`
- Create Address entity with fields:
  - `Id`
  - `UserId`
  - `Street`
  - `City`
  - `State`
  - `Country`
  - `PostalCode`
- Link address to user.
- Store in database.

---

#### 4. **View Addresses**
- Endpoint: `GET /profile/addresses`
- Fetch all addresses linked to logged-in user.
- Return list of saved addresses.

---

#### 5. **JWT Security**
- Protect all endpoints using `[Authorize]`
- Extract `UserId` from JWT claims instead of passing manually.
- Ensure users can only access their own profile data.

---

### 🧪 Postman Testing

**Objective:** Verify profile operations and JWT-based security.

---

#### ✅ Test Cases

| Request | Description | Expected Response |
|--------|-------------|------------------|
| `GET /profile/{id}` | Fetch user profile | `200 OK` |
| `PUT /profile/update` | Update profile details | `200 OK` |
| `POST /profile/address` | Add new address | `201 Created` |
| `GET /profile/addresses` | Get user addresses | `200 OK` |
| Access without JWT | Unauthorized access | `401 Unauthorized` |

---

#### 🔹 Sample Update Profile Request
```json
{
  "fullName": "Sankalp Agarwal",
  "email": "sankalp_updated@gmail.com"
}
