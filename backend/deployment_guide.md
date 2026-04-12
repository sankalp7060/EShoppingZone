# EShoppingZone Backend Deployment Guide

This guide explains how to deploy your microservices to **Fly.io** and **Render.com**.

## 1. Push Code to GitHub
Ensure all your backend changes (Dockerfiles, fly.toml, Program.cs) are pushed to your GitHub repository.

## 2. Deploy Order & Product Services + Gateway (Fly.io)

### Prerequisites
- Install [Fly CLI](https://fly.io/docs/hands-on/install-flyctl/)
- Run `fly auth login`

### Steps for each Fly app:
For `api-gateway`, `services/order-service/src/EShoppingZone.Order.API`, and `services/product-service/src/EShoppingZone.Product.API`:

1.  **Navigate** to the folder in your terminal.
2.  **Initialize**: Run `fly launch`.
    -   When asked to use existing config (`fly.toml`), say **Yes**.
    -   Choose a unique app name or keep the default.
    -   Select your preferred region.
    -   When asked to set up a Postgres database or Redis, say **No** (we use Supabase).
    -   When asked to deploy, say **No** for now (we need to set secrets first).
3.  **Set Secrets**: Run the following command for each service to add your Supabase connection:
    ```bash
    fly secrets set ConnectionStrings__DefaultConnection="YOUR_SUPABASE_CONNECTION_STRING"
    ```
4.  **Set JWT Secrets**:
    ```bash
    fly secrets set Jwt__Key="YOUR_SUPER_SECURE_KEY" Jwt__Issuer="EShoppingZone" Jwt__Audience="EShoppingZoneClients"
    ```
5.  **Deploy**: Run `fly deploy`.

---

## 3. Deploy Cart, Profile, & Wallet Services (Render.com)

### Steps:
1.  Log in to [Render Dashboard](https://dashboard.render.com/).
2.  Click **New +** -> **Web Service**.
3.  Connect your GitHub repository.
4.  **Configure each service**:
    -   **Name**: `eshoppingzone-cart-service`, etc.
    -   **Root Directory**: For Cart, set to `backend/`.
    -   **Language**: `Docker`.
    -   **Dockerfile Path**: `services/cart-service/src/EShoppingZone.Cart.API/Dockerfile`.
5.  **Add Environment Variables** (Under "Env Vars" tab):
    -   `ConnectionStrings__DefaultConnection`: Your Supabase URI.
    -   `Jwt__Key`: Your JWT Key.
    -   `Jwt__Issuer`: `EShoppingZone`.
    -   `Jwt__Audience`: `EShoppingZoneClients`.
    -   `ASPNETCORE_URLS`: `http://0.0.0.0:8080`.
6.  **Create Web Service**.

---

## 4. Final step: Linking everything in the Gateway

Once all services are deployed, you will have URLs like:
- `https://eshoppingzone-profile.onrender.com`
- `https://eshoppingzone-order.fly.dev`

**Go to your Fly.io Gateway Dashboard (or use CLI)** and set the following secrets so the Gateway knows where to find them:

```bash
fly secrets set ReverseProxy__Clusters__profile-cluster__Destinations__destination1__Address="https://PROFILE_URL/"
fly secrets set ReverseProxy__Clusters__product-cluster__Destinations__destination1__Address="https://PRODUCT_URL/"
fly secrets set ReverseProxy__Clusters__cart-cluster__Destinations__destination1__Address="https://CART_URL/"
fly secrets set ReverseProxy__Clusters__order-cluster__Destinations__destination1__Address="https://ORDER_URL/"
fly secrets set ReverseProxy__Clusters__wallet-cluster__Destinations__destination1__Address="https://WALLET_URL/"
```

**Finally, update your Vercel Frontend `VITE_API_BASE_URL`** to your new Gateway URL:
- `https://eshoppingzone-gateway.fly.dev`
