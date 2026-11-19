# OPS-101 — Authentication

## Implementation

OpsPilot uses JWT Bearer authentication.

- ASP.NET Core JWT Bearer authentication
- PostgreSQL user persistence
- PasswordHasher<User> for password hashing
- 30-minute access-token lifetime
- Issuer, audience, signing-key and expiry validation
- Inactive users cannot authenticate

## API Endpoints

| Method | Endpoint | Access |
|---|---|---|
| POST | /api/auth/login | Anonymous |
| GET | /api/auth/me | Authenticated |
| GET | /health/live | Anonymous |
| GET | /health/ready | Anonymous |

## Login Flow

1. Client submits email and password.
2. API retrieves the user from PostgreSQL.
3. PasswordHasher verifies the password.
4. API generates a signed JWT.
5. Client sends the token in the Authorization header.
6. API validates the token before granting access.

## Configuration

Required environment variables:

- Jwt__Secret
- Jwt__Issuer
- Jwt__Audience
- Jwt__AccessTokenMinutes

Demo user configuration for Development:

- DemoUser__Email
- DemoUser__Password

Never commit actual credentials or signing secrets.

## Tests

Run:

dotnet test tests/api-dotnet/OpsPilot.Api.Tests/OpsPilot.Api.Tests.csproj