using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations
{
    /// <inheritdoc />
    public partial class UserBikeActivityImport2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_bike_activities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sport = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    start_time_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_seconds = table.Column<double>(type: "double precision", nullable: true),
                    distance_meters = table.Column<double>(type: "double precision", nullable: true),
                    calories = table.Column<double>(type: "double precision", nullable: true),
                    avg_heart_rate_bpm = table.Column<int>(type: "integer", nullable: true),
                    max_heart_rate_bpm = table.Column<int>(type: "integer", nullable: true),
                    intensity = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    trigger_method = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    imported_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    trackpoint_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_bike_activities", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_bike_activities_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_bike_activity_trackpoints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_no = table.Column<int>(type: "integer", nullable: false),
                    time_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    altitude_m = table.Column<double>(type: "double precision", nullable: true),
                    heart_rate_bpm = table.Column<int>(type: "integer", nullable: true),
                    cadence = table.Column<int>(type: "integer", nullable: true),
                    speed_m_s = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_bike_activity_trackpoints", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_bike_activity_trackpoints_user_bike_activities_activit~",
                        column: x => x.activity_id,
                        principalTable: "user_bike_activities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_bike_activities_user_id_start_time_utc",
                table: "user_bike_activities",
                columns: new[] { "user_id", "start_time_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_user_bike_activity_trackpoints_activity_id_order_no",
                table: "user_bike_activity_trackpoints",
                columns: new[] { "activity_id", "order_no" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_bike_activity_trackpoints");

            migrationBuilder.DropTable(
                name: "user_bike_activities");
        }
    }
}
