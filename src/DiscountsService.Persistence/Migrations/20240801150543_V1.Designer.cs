﻿// <auto-generated />
using System;
using DiscountsService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace DiscountsService.Persistence.Migrations
{
    [DbContext(typeof(DiscountsDbContext))]
    [Migration("20240801150543_V1")]
    partial class V1
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("DiscountsService.Persistence.DiscountCode", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasMaxLength(8)
                        .HasColumnType("varchar(8)")
                        .HasAnnotation("MinLength", 7);

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("Used")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime?>("UsedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.ToTable("DiscountCodes", t =>
                        {
                            t.HasCheckConstraint("CK_Code_Alphanumeric", "Code REGEXP '^[A-Za-z0-9]+$'");

                            t.HasCheckConstraint("CK_Code_Length", "LENGTH(Code) >= 7 AND LENGTH(Code) <= 8");
                        });
                });
#pragma warning restore 612, 618
        }
    }
}
