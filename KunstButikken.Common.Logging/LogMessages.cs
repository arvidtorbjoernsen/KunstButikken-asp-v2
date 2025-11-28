using System;
using Microsoft.Extensions.Logging;

/// <summary>
/// Centralized LoggerMessage delegates to keep call sites small and avoid repeating delegate declarations across files.
/// Add more delegates here as the project adopts centralized logging messages.
/// </summary>
namespace KunstButikken.Common.Logging;

public static class LogMessages
{
    // Keycloak / seeding related
    public static readonly Action<ILogger, string, Exception?> HttpRequestFailed =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(2100, "HttpRequestFailed"),
            "HTTP request failed for {Username}");

    public static readonly Action<ILogger, string, Exception?> JsonProcessingFailed =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(2101, "JsonProcessingFailed"),
            "JSON processing failed for {Username}");

    public static readonly Action<ILogger, string, Exception?> OperationCanceled =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(2102, "OperationCanceled"),
            "Operation canceled for {Username}");

    public static readonly Action<ILogger, string, Exception?> DbUpdateFailed =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(2103, "DbUpdateFailed"),
            "Database update failed for {Username}");

    public static readonly Action<ILogger, string, Exception?> UnexpectedError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(2104, "UnexpectedError"),
            "Unexpected error while seeding {Username}");

    // Auto-generated delegates (centralized) - unique names
    public static readonly Action<ILogger, Exception?> Warning_Msg_3000 =
        LoggerMessage.Define(LogLevel.Warning, new EventId(3000, "Warning_Msg_3000"), "ParseKeycloakUsers: malformed JSON from Keycloak");

    public static readonly Action<ILogger, Exception?> Warning_Msg_3001 =
        LoggerMessage.Define(LogLevel.Warning, new EventId(3001, "Warning_Msg_3001"), "ParseKeycloakUsers: unexpected error parsing JSON from Keycloak");

    public static readonly Action<ILogger, Exception?> Warning_Msg_3002 =
        LoggerMessage.Define(LogLevel.Warning, new EventId(3002, "Warning_Msg_3002"), "ParseRolesFromJson: malformed roles JSON");

    public static readonly Action<ILogger, Exception?> Warning_Msg_3003 =
        LoggerMessage.Define(LogLevel.Warning, new EventId(3003, "Warning_Msg_3003"), "ParseRolesFromJson: unexpected error parsing roles JSON");

    public static readonly Action<ILogger, string, Exception?> Warning_Id_3004 =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(3004, "Warning_Id_3004"), "[KeycloakSync] HTTP error fetching roles for user {Id}");

    public static readonly Action<ILogger, string, Exception?> Warning_Id_3005 =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(3005, "Warning_Id_3005"), "[KeycloakSync] Error fetching roles for user {Id}");

    // Change these generated attempt/attempts delegates to accept ints (caller code passes ints)
    public static readonly Action<ILogger, int, int, Exception?> Warning_Attempt_Attempts_3006 =
        LoggerMessage.Define<int, int>(LogLevel.Warning, new EventId(3006, "Warning_Attempt_Attempts_3006"), "RetryAsync: HTTP request failed on attempt {Attempt}/{Attempts}");

    public static readonly Action<ILogger, int, int, Exception?> Warning_Attempt_Attempts_3007 =
        LoggerMessage.Define<int, int>(LogLevel.Warning, new EventId(3007, "Warning_Attempt_Attempts_3007"), "RetryAsync: JSON parsing failed on attempt {Attempt}/{Attempts}");

    public static readonly Action<ILogger, int, int, Exception?> Warning_Attempt_Attempts_3008 =
        LoggerMessage.Define<int, int>(LogLevel.Warning, new EventId(3008, "Warning_Attempt_Attempts_3008"), "RetryAsync: Database update failed on attempt {Attempt}/{Attempts}");

    public static readonly Action<ILogger, int, int, Exception?> Warning_Attempt_Attempts_3009 =
        LoggerMessage.Define<int, int>(LogLevel.Warning, new EventId(3009, "Warning_Attempt_Attempts_3009"), "RetryAsync: operation failed on attempt {Attempt}/{Attempts}");

    public static readonly Action<ILogger, string, Exception?> Warning_Key_3010 =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(3010, "Warning_Key_3010"), "Readiness check for {Key} failed");


    // Auto-generated delegates
    public static readonly Action<ILogger, Exception?> Information_Msg_1 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3000, "Information_Msg_1"), "[PaymentService] Database is ready");

    // Payment attempt: accept int attempts
    public static readonly Action<ILogger, int, Exception?> Warning_Attempt_2 =
        LoggerMessage.Define<int>(LogLevel.Warning, new EventId(3001, "Warning_Attempt_2"), "[PaymentService] Database connection attempt {Attempt} failed");

    // Max retries as int
    public static readonly Action<ILogger, int, Exception?> Error_MaxRetries_3 =
        LoggerMessage.Define<int>(LogLevel.Error, new EventId(3002, "Error_MaxRetries_3"), "[PaymentService] Database connection failed after {MaxRetries} attempts");

    public static readonly Action<ILogger, Exception?> Warning_Msg_4 =
        LoggerMessage.Define(LogLevel.Warning, new EventId(3003, "Warning_Msg_4"), "[PaymentService] Service starting without database connection");

    public static readonly Action<ILogger, Exception?> Warning_Msg_5 =
        LoggerMessage.Define(LogLevel.Warning, new EventId(3004, "Warning_Msg_5"), "Readiness check: Keycloak HTTP request failed");

    public static readonly Action<ILogger, Exception?> Information_Msg_6 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3005, "Information_Msg_6"), "[DbMigration] Starting database migration check...");

    public static readonly Action<ILogger, Exception?> Information_Msg_7 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3006, "Information_Msg_7"), "[DbMigration] Hosted service stopped.");

    public static readonly Action<ILogger, Exception?> Information_Msg_8 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3007, "Information_Msg_8"), "Starting Keycloak seeding service...");

    public static readonly Action<ILogger, int, Exception?> Information_Attempt_9 =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(3008, "Information_Attempt_9"), "Successfully completed seeding on attempt {Attempt}");

    public static readonly Action<ILogger, int, double, Exception?> Warning_Attempt_Delay_10 =
        LoggerMessage.Define<int, double>(LogLevel.Warning, new EventId(3009, "Warning_Attempt_Delay_10"), "Attempt {Attempt} failed while seeding Keycloak. Retrying in {Delay} seconds...");

    public static readonly Action<ILogger, int, Exception?> Error_MaxRetries_11 =
        LoggerMessage.Define<int>(LogLevel.Error, new EventId(3010, "Error_MaxRetries_11"), "Failed after {MaxRetries} attempts. Service will start without Keycloak seeding.");

    public static readonly Action<ILogger, Exception?> Information_Msg_12 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3011, "Information_Msg_12"), "Keycloak seeding service stopped.");

    public static readonly Action<ILogger, Exception?> Information_Msg_13 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3012, "Information_Msg_13"), "[DbMigration] Running database migrations...");

    public static readonly Action<ILogger, Exception?> Information_Msg_14 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3013, "Information_Msg_14"), "[DbMigration] Applying EF migrations...");

    public static readonly Action<ILogger, Exception?> Information_Msg_15 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3014, "Information_Msg_15"), "[DbMigration] EF migrations applied successfully.");

    public static readonly Action<ILogger, Exception?> Information_Msg_16 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3015, "Information_Msg_16"), "[DbMigration] No pending migrations. Ensuring database is created.");

    public static readonly Action<ILogger, string, Exception?> Error_Message_17 =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(3016, "Error_Message_17"), "[DbMigration] Postgres error during migration: {Message}");

    public static readonly Action<ILogger, string, Exception?> Error_Message_18 =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(3017, "Error_Message_18"), "[DbMigration] Unexpected error during migration: {Message}");

    public static readonly Action<ILogger, Exception?> Information_Msg_19 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3018, "Information_Msg_19"), "[AuctionSeeding] Starting Auction seeding service...");

    public static readonly Action<ILogger, Exception?> Information_Msg_20 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3019, "Information_Msg_20"), "[AuctionSeeding] Auction seeding service stopped.");

    public static readonly Action<ILogger, Exception?> Warning_Msg_21 =
        LoggerMessage.Define(LogLevel.Warning, new EventId(3020, "Warning_Msg_21"), "[AuctionSeeding] Failed to migrate DB before seeding. Continuing to check emptiness.");

    public static readonly Action<ILogger, Exception?> Error_Msg_22 =
        LoggerMessage.Define(LogLevel.Error, new EventId(3021, "Error_Msg_22"), "[AuctionSeeding] Error checking if Auctions table is empty. Skipping seeding.");

    public static readonly Action<ILogger, Exception?> Information_Msg_23 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3022, "Information_Msg_23"), "[AuctionSeeding] Skipping: Auctions table already has data.");

    public static readonly Action<ILogger, Exception?> Warning_Msg_24 =
        LoggerMessage.Define(LogLevel.Warning, new EventId(3023, "Warning_Msg_24"), "[AuctionSeeding] ART_SERVICE_URL not configured. Cannot seed auctions.");

    // Accept ints and double for attempt/maxAttempts/delay
    public static readonly Action<ILogger, int, int, double, Exception?> Warning_Attempt_MaxAttempts_Delay_25 =
        LoggerMessage.Define<int, int, double>(LogLevel.Warning, new EventId(3024, "Warning_Attempt_MaxAttempts_Delay_25"), "[AuctionSeeding] Attempt {Attempt}/{MaxAttempts}: No art available yet. Retrying in {Delay}s...");

    public static readonly Action<ILogger, int, int, double, Exception?> Warning_Attempt_MaxAttempts_Delay_26 =
        LoggerMessage.Define<int, int, double>(LogLevel.Warning, new EventId(3025, "Warning_Attempt_MaxAttempts_Delay_26"), "[AuctionSeeding] Attempt {Attempt}/{MaxAttempts}: Error fetching art. Retrying in {Delay}s...");

    public static readonly Action<ILogger, Exception?> Information_Msg_27 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3026, "Information_Msg_27"), "[AuctionSeeding] Auctions appeared during retry window. Aborting seeding.");

    public static readonly Action<ILogger, Exception?> Warning_Msg_28 =
        LoggerMessage.Define(LogLevel.Warning, new EventId(3027, "Warning_Msg_28"), "[AuctionSeeding] Giving up: Could not retrieve any art to seed auctions.");

    // Count as int
    public static readonly Action<ILogger, int, Exception?> Information_Count_29 =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(3028, "Information_Count_29"), "[AuctionSeeding] Seeded {Count} auctions.");

    // ArtId as Guid (callers pass Guid)
    public static readonly Action<ILogger, Guid, Exception?> Warning_ArtId_30 =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(3029, "Warning_ArtId_30"), "Art with id {ArtId} not found");

    public static readonly Action<ILogger, Exception?> Warning_Msg_31 =
        LoggerMessage.Define(LogLevel.Warning, new EventId(3030, "Warning_Msg_31"), "Readiness: db check failed");

    public static readonly Action<ILogger, Exception?> Information_Msg_32 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3031, "Information_Msg_32"), "[ArtSeeding] Starting Art seeding service...");

    public static readonly Action<ILogger, Exception?> Information_Msg_33 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3032, "Information_Msg_33"), "[ArtSeeding] Art seeding service stopped.");

    public static readonly Action<ILogger, Exception?> Warning_Msg_34 =
        LoggerMessage.Define(LogLevel.Warning, new EventId(3033, "Warning_Msg_34"), "[ArtSeeding] Arts table not found. Assuming empty for seeding.");

    public static readonly Action<ILogger, Exception?> Error_Msg_35 =
        LoggerMessage.Define(LogLevel.Error, new EventId(3034, "Error_Msg_35"), "[ArtSeeding] Error checking if Arts table is empty. Skipping seeding.");

    public static readonly Action<ILogger, Exception?> Information_Msg_36 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3035, "Information_Msg_36"), "[ArtSeeding] Skipping seeding: Arts table already has data.");

    public static readonly Action<ILogger, Exception?> Information_Msg_37 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3036, "Information_Msg_37"), "[ArtSeeding] Seeding up to 10 art pieces because DB was empty...");

    public static readonly Action<ILogger, Exception?> Warning_Msg_38 =
        LoggerMessage.Define(LogLevel.Warning, new EventId(3037, "Warning_Msg_38"), "[ArtSeeding] Could not verify table columns. Assuming schema is correct.");

    public static readonly Action<ILogger, Exception?> Warning_Msg_39 =
        LoggerMessage.Define(LogLevel.Warning, new EventId(3038, "Warning_Msg_39"), "[ArtSeeding] Could not fetch sellers from UserService. Using fallback deterministic data.");

    // Count as int
    public static readonly Action<ILogger, int, Exception?> Information_Count_40 =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(3039, "Information_Count_40"), "[ArtSeeding] Successfully fetched {Count} sellers from UserService.");

    public static readonly Action<ILogger, Exception?> Error_Msg_41 =
        LoggerMessage.Define(LogLevel.Error, new EventId(3040, "Error_Msg_41"), "[ArtSeeding] Error reading or deserializing art_metadata.json. Seeding will proceed without metadata.");

    public static readonly Action<ILogger, Exception?> Warning_Msg_42 =
        LoggerMessage.Define(LogLevel.Warning, new EventId(3041, "Warning_Msg_42"), "[ArtSeeding] IBlobStorage service not available. Images will not be uploaded to blob storage.");

    public static readonly Action<ILogger, string, Exception?> Error_Filename_43 =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(3042, "Error_Filename_43"), "[ArtSeeding] Error uploading image {Filename} to blob storage. Using placeholder image.");

    // Count as int
    public static readonly Action<ILogger, int, Exception?> Information_Count_44 =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(3043, "Information_Count_44"), "[ArtSeeding] Seeding completed successfully. {Count} art pieces added.");

    public static readonly Action<ILogger, Exception?> Information_Msg_45 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3044, "Information_Msg_45"), "[ArtSeeding] Ensuring ArtService database is migrated...");

    public static readonly Action<ILogger, Exception?> Information_Msg_46 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3045, "Information_Msg_46"), "[ArtSeeding] Applying EF migrations...");

    public static readonly Action<ILogger, Exception?> Information_Msg_47 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3046, "Information_Msg_47"), "[ArtSeeding] EF migrations applied successfully.");

    public static readonly Action<ILogger, Exception?> Information_Msg_48 =
        LoggerMessage.Define(LogLevel.Information, new EventId(3047, "Information_Msg_48"), "[ArtSeeding] No pending migrations. Ensuring database is created.");

    public static readonly Action<ILogger, string, Exception?> Error_Message_49 =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(3048, "Error_Message_49"), "[ArtSeeding] Fatal error during database migration after multiple retries. Message: {Message}");

    public static readonly Action<ILogger, Exception?> Warning_Msg_50 =
        LoggerMessage.Define(LogLevel.Warning, new EventId(3049, "Warning_Msg_50"), "[ArtSeeding] USER_SERVICE_URL not configured. Cannot fetch seller IDs.");

    public static readonly Action<ILogger, int, int, double, Exception?> Warning_Attempt_MaxAttempts_Delay_51 =
        LoggerMessage.Define<int, int, double>(LogLevel.Warning, new EventId(3050, "Warning_Attempt_MaxAttempts_Delay_51"), "[ArtSeeding] Attempt {Attempt}/{MaxAttempts}: No sellers available yet. Retrying in {Delay}s...");

    public static readonly Action<ILogger, int, int, double, Exception?> Warning_Attempt_MaxAttempts_Delay_52 =
        LoggerMessage.Define<int, int, double>(LogLevel.Warning, new EventId(3051, "Warning_Attempt_MaxAttempts_Delay_52"), "[ArtSeeding] Attempt {Attempt}/{MaxAttempts}: HTTP error fetching sellers. Retrying in {Delay}s...");

    public static readonly Action<ILogger, int, int, double, Exception?> Warning_Attempt_MaxAttempts_Delay_53 =
        LoggerMessage.Define<int, int, double>(LogLevel.Warning, new EventId(3052, "Warning_Attempt_MaxAttempts_Delay_53"), "[ArtSeeding] Attempt {Attempt}/{MaxAttempts}: Request canceled while fetching sellers. Retrying in {Delay}s...");

    public static readonly Action<ILogger, int, Exception?> Warning_MaxAttempts_54 =
        LoggerMessage.Define<int>(LogLevel.Warning, new EventId(3053, "Warning_MaxAttempts_54"), "[ArtSeeding] Giving up: Could not retrieve sellers from UserService after {MaxAttempts} attempts.");

    public static readonly Action<ILogger, Guid, Exception?> Information_AuctionId_55 =
        LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(3054, "Information_AuctionId_55"), "Auction ended: {AuctionId}");

    // Convenience aliases for older/shorter names used across the codebase
    public static readonly Action<ILogger, Exception?> Warning_Msg = Warning_Msg_3001;
    public static readonly Action<ILogger, string, Exception?> Warning_Id = Warning_Id_3005;
    public static readonly Action<ILogger, int, int, Exception?> Warning_Attempt_Attempts = Warning_Attempt_Attempts_3006;
    public static readonly Action<ILogger, string, Exception?> Warning_Key = Warning_Key_3010;

}
