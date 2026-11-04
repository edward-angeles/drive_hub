# Drive Hub — Firebase milestone 1

ASP.NET Core MVC + C# + **Google Cloud Firestore**. No SQLite/EF Core dependency remains.

## Firebase setup (do these in order)
1. Visit https://console.firebase.google.com/ and create a project named Drive Hub (project ID may differ).
2. In **Build > Firestore Database**, create a database. Select a nearby region and **Production mode**. Note: Firestore region cannot easily be changed later. Billing/quotas depend on your project and region.
3. In **Project settings > Service accounts**, generate a **Firebase Admin SDK private key** JSON. Store it OUTSIDE your Git repository, e.g. `C:\DriveHubSecrets\serviceAccountKey.json`. Never upload or share this file.
4. On Windows PowerShell, in the project directory, run:
   ```powershell
   dotnet user-secrets init
   dotnet user-secrets set "Firebase:ProjectId" "YOUR_FIREBASE_PROJECT_ID"
   $env:GOOGLE_APPLICATION_CREDENTIALS = "C:\DriveHubSecrets\serviceAccountKey.json"
   dotnet restore
   dotnet run
   ```
   Environment variables set this way last for the current PowerShell session only. To run via Visual Studio, set `GOOGLE_APPLICATION_CREDENTIALS` in its launch environment (never commit credential paths with secrets).
5. Open the printed localhost URL, go to `/Parts/Manage`, add a part, then visit `/Parts`. Confirm the `parts` collection appears in Firestore Console.

## Notes
- Firestore uses string document IDs, not SQLite auto-increment integers.
- Prices are stored as integer `priceCents` to avoid floating point currency errors.
- The Firestore server SDK authenticates with a service account and bypasses client-side Firestore security rules. **This is an unsecured local teaching demo only.** Seller endpoints must be protected with ASP.NET authentication/authorization and ownership checks before deployment. Never expose service-account credentials to the browser.
- No sample data is seeded automatically: create the first product using the seller form.
- Search/filter is performed in application memory for this small demonstration dataset. Use indexed Firestore queries/pagination for a large catalog.
- Firebase Authentication, buyer/seller accounts, carts, orders, API and vehicle compatibility are later milestones.
- Firestore may require enabling the relevant Google Cloud API. The Firestore console setup normally handles this.
- If application control blocks .NET build artifacts on your machine, use the previously tested `dotnet run --artifacts-path "$env:TEMP/CollegeMVCArtifacts"` workaround.
- Suggested Git commit: `git commit -m "Replace SQLite persistence with Firestore product repository"`.

## Security before release
Add role-based authentication, enforce seller ownership on every mutation, validate input, use server-side checkout transactions, and avoid public access to management actions. Use separate development and production Firebase projects where practical.
