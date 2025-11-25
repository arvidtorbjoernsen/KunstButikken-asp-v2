# Database Management Scripts

This folder contains scripts for managing the development databases.

## Available Scripts

### reset-databases.sh (Recommended)

**Simple and fast database reset by removing the .data folder**

```bash
./scripts/reset-databases.sh
```

This script:
- Removes the entire `.data` folder
- Clears all databases, Keycloak data, and Azurite blob storage
- **Fastest and simplest way to get a fresh start**
- Automatically uses `sudo` if permission errors occur

**When to use:**
- You want a complete fresh start
- You're experiencing data inconsistencies
- You want to test the seeding process
- After making changes to seeding logic

**Note:** You may be prompted for your password if Keycloak has created protected files.

### reset-databases-advanced.sh (Best for Permission Issues)

**Stops Docker containers first, then removes .data folder**

```bash
./scripts/reset-databases-advanced.sh
```

This script:
- Stops running Docker containers (Keycloak, PostgreSQL, Azurite)
- Waits for containers to release file locks
- Changes file permissions before deletion
- Uses `sudo` when needed
- **Best option if you get permission errors**

**When to use:**
- You got permission denied errors with the simple script
- Files are locked by running containers
- You want a more thorough cleanup
- You're having persistent file access issues

### drop-all-tables.sh (Advanced)

**Drops all tables in all databases using SQL**

```bash
./scripts/drop-all-tables.sh
```

This script:
- Connects to PostgreSQL directly
- Drops all tables, sequences, and views in each database
- Keeps the database structure but removes all data
- Requires PostgreSQL credentials

**Configuration:**
You can set these environment variables before running:
```bash
export PGHOST=localhost
export PGPORT=5432
export PGUSER=postgres
export PGPASSWORD=postgres
./scripts/drop-all-tables.sh
```

**When to use:**
- You want more control over the reset process
- You want to keep databases but clear tables
- The .data folder method doesn't work for some reason

## After Running Any Script

1. **Restart the application:**
   ```bash
   dotnet run --project KunstButikken.AppHost
   ```

2. **Wait for automatic seeding:**
   - UserService will sync with Keycloak
   - ArtService will fetch sellers and seed art
   - AuctionService will create sample auctions

3. **Verify in logs:**
   ```
   [KeycloakSync] Created profile for seller1 (DisplayName: Selma Selger, Seller: True, Admin: False)
   [ArtSeeding] Successfully fetched 3 sellers from UserService.
   [ArtSeeding] Seeding completed successfully. 10 art pieces added.
   [AuctionSeeding] Seeded 5 auctions.
   ```

## Test Users

After seeding, you can log in with these test accounts:

| Username | Email | Password | Role |
|----------|-------|----------|------|
| admin | admin@example.com | admin | Admin |
| seller1 | seller1@example.com | seller | Seller |
| seller2 | seller2@example.com | seller | Seller |
| seller3 | seller3@example.com | seller | Seller |
| buyer1 | buyer1@example.com | buyer | Buyer |
| buyer2-7 | buyer2-7@example.com | buyer | Buyer |

## Troubleshooting

### Permission Denied Errors

**Problem:** `rm: .data/keycloak/import: Permission denied`

**Why:** Keycloak container creates files with root ownership that require elevated permissions to delete.

**Solutions:**
1. **Easiest:** The script will automatically use `sudo` - just enter your password when prompted
2. **Alternative:** Use the advanced script that stops containers first:
   ```bash
   ./scripts/reset-databases-advanced.sh
   ```
3. **Manual:** Stop the app, then:
   ```bash
   sudo rm -rf .data
   ```

### Script won't run (Permission Denied)
```bash
chmod +x scripts/*.sh
```

### PostgreSQL connection issues
Make sure PostgreSQL is running:
```bash
# Check if PostgreSQL is running
psql -h localhost -U postgres -l
```

### Seeding doesn't work after reset
1. Check the logs for errors
2. Make sure all services are running
3. Verify environment variables are set correctly
4. Try the reset-databases.sh script instead

### Seller display names still show as usernames
This means the art was seeded before the Keycloak sync completed:
1. Run the reset script again
2. Wait a few seconds after startup before checking
3. The UserService sync should complete before ArtService seeding

## Common Workflows

### Fresh Start for Testing
```bash
./scripts/reset-databases.sh
dotnet run --project KunstButikken.AppHost
# Wait 10-15 seconds for seeding to complete
# Navigate to http://localhost:3000
```

### Reset Just the Data (Keep Keycloak Users)
```bash
./scripts/drop-all-tables.sh
# Answer 'YES' when prompted
dotnet run --project KunstButikken.AppHost
```

### Emergency Full Reset
```bash
# Stop the application (Ctrl+C)
rm -rf .data
dotnet run --project KunstButikken.AppHost
```

