using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KunstButikken.AuctionService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Arts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Artist = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    SellerId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Arts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Auctions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerDisplayName = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartingPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    ReservePrice = table.Column<decimal>(type: "numeric", nullable: true),
                    WinningBid = table.Column<decimal>(type: "numeric", nullable: true),
                    WinnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auctions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bids",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuctionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BidderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    PlacedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bids", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bids_Auctions_AuctionId",
                        column: x => x.AuctionId,
                        principalTable: "Auctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bids_AuctionId",
                table: "Bids",
                column: "AuctionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Arts");

            migrationBuilder.DropTable(
                name: "Bids");

            migrationBuilder.DropTable(
                name: "Auctions");
        }
    }
}
