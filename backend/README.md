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
