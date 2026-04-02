# PayFlow API: Interactive Swagger Testing Guide

This guide will walk you through verifying the complete lifecycle of a payment flow using your local Swagger UI at [http://localhost:8080/swagger](http://localhost:8080/swagger).

> [!TIP]
> Keep this document open side-by-side with your Swagger UI to easily copy and paste the UUIDs and JSON payloads.

---

## Step 1: Create the Sender's Wallet
First, we need to create a wallet to hold the money being sent. We will use the first dummy user I registered for you in the database.

1. Expand the **`POST /api/v1/wallets`** endpoint.
2. Click **Try it out**.
3. Use the following JSON payload:
```json
{
  "userId": "00000000-0000-0000-0000-000000000001",
  "currency": "USD"
}
```
4. Click **Execute**.
5. Look at the `201 Created` Response Body. **Copy the generated `"id"` value** (e.g., `e4c7b8d1-1234-...`). This is `Wallet ID 1`.

---

## Step 2: Create the Receiver's Wallet
Next, we need a second wallet to receive the money, belonging to a second user.

1. Still in the **`POST /api/v1/wallets`** endpoint...
2. Replace the JSON payload with:
```json
{
  "userId": "00000000-0000-0000-0000-000000000002",
  "currency": "USD"
}
```
3. Click **Execute**.
4. Look at the new `201 Created` Response Body. **Copy the generated `"id"` value**. This is `Wallet ID 2`.

---

## Step 3: Top-Up the Sender's Wallet
Now, let's pretend User 1 deposits $500.00 into their wallet from their bank account.
*(Note: Amounts in our system are stored in cents to prevent decimal math errors. So $500.00 = `50000`)* 

1. Expand the **`POST /api/v1/wallets/{id}/topup`** endpoint.
2. Click **Try it out**.
3. In the **`id`** parameter box, paste **Wallet ID 1**.
4. In the **`Idempotency-Key`** header box, type any random string (e.g., `test-topup-001`).
5. Use the following JSON payload:
```json
{
  "amount": 50000,
  "referenceId": "bank-transfer-xyz"
}
```
6. Click **Execute**. You should get a `200 OK` showing the Top-Up transaction!

---

## Step 4: Transfer Money to the Receiver
Let's send $150.00 from Wallet 1 to Wallet 2.

1. Expand the **`POST /api/v1/payments/transfer`** endpoint.
2. Click **Try it out**.
3. In the **`Idempotency-Key`** header box, type a new random string (e.g., `test-transfer-001`).
4. Replace the JSON payload with:
*(Make sure to swap in your actual copied Wallet IDs)*
```json
{
  "sourceWalletId": "<Paste Wallet ID 1 Here>",
  "destinationWalletId": "<Paste Wallet ID 2 Here>",
  "amount": 15000,
  "description": "Lunch money!"
}
```
5. Click **Execute**. You should get a `200 OK` response showing a successful transaction.

---

## Step 5: Verify the Balances!
Let's make sure the math actually worked perfectly. 
Wallet 1 should now have exactly $350.00 (`35000`) and Wallet 2 should have exactly $150.00 (`15000`).

1. Expand the **`GET /api/v1/wallets/{id}`** endpoint.
2. Click **Try it out**.
3. Paste **Wallet ID 1** into the ID box and click **Execute**. Confirm the `balance` field is `35000`.
4. Replace the ID box with **Wallet ID 2** and click **Execute**. Confirm the `balance` is `15000`.

---

## Step 6: Test Idempotency (Optional)
Return to **Step 4** (the Transfer). 
Try clicking the **Execute** button a second time using the exact same JSON payload and exact same `Idempotency-Key` (`test-transfer-001`).

You will notice it returns rapidly with a `200 OK`, but if you look closely, the database wasn't actually touched again. If you check the Wallet 1 balance, it shouldn't have been charged a second time! The system saw the duplicate receipt and protected the user automatically.
