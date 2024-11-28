﻿// <auto-generated />
using System;
using ClassRegistrationWorker;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClassRegistrationWorker.Migrations
{
    [DbContext(typeof(ClassesDbContext))]
    [Migration("20241128195007_PaymentReminderToken")]
    partial class PaymentReminderToken
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("ClassRegistrationWorker.ClassRegistration", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<Guid>("ClassId")
                        .HasColumnType("uuid");

                    b.Property<bool>("IsCompleted")
                        .HasColumnType("boolean");

                    b.Property<Guid>("PaymentId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("RegistrationId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("ReservationId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("StudentId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.ToTable("ClassRegistrations");
                });

            modelBuilder.Entity("ClassRegistrationWorker.StateMachines.ClassRegistrationState", b =>
                {
                    b.Property<Guid>("CorrelationId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("ClassId")
                        .HasColumnType("uuid");

                    b.Property<string>("CurrentState")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<Guid?>("PaymenRemindertTokenId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("PaymentId")
                        .HasColumnType("uuid");

                    b.Property<string>("PaymentLink")
                        .HasColumnType("text");

                    b.Property<Guid>("ReservationId")
                        .HasColumnType("uuid");

                    b.Property<uint>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.Property<Guid>("StudentId")
                        .HasColumnType("uuid");

                    b.HasKey("CorrelationId");

                    b.ToTable("ClassRegistrationState");
                });
#pragma warning restore 612, 618
        }
    }
}
