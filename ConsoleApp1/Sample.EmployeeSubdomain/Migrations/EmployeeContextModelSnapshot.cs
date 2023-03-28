﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sample.EmployeeSubdomain.DatabaseContext;

#nullable disable

namespace Sample.EmployeeSubdomain.Migrations
{
    [DbContext(typeof(EmployeeContext))]
    partial class EmployeeContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("Sample.EmployeeSubdomain.Entities.EmployeeEntity", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Employees");
                });

            modelBuilder.Entity("Sample.Sdk.EntityModel.InComingEventEntity", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("AckQueueName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("AcknowledgementEndpoint")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Body")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CertificateKey")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CertificateLocation")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("CreationTime")
                        .HasColumnType("bigint");

                    b.Property<string>("CryptoEndpoint")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DecryptEndpoint")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<string>("MessageKey")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MsgDecryptScope")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MsgQueueEndpoint")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MsgQueueName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Scheme")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ServiceInstanceId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SignDataKeyId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Version")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("WasAcknowledge")
                        .HasColumnType("bit");

                    b.Property<bool>("WasProcessed")
                        .HasColumnType("bit");

                    b.Property<string>("WellknownEndpoint")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("InComingEvents");
                });

            modelBuilder.Entity("Sample.Sdk.EntityModel.OutgoingEventEntity", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("AckQueueName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Body")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CertificateKey")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CertificateLocation")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("CreationTime")
                        .HasColumnType("bigint");

                    b.Property<string>("CryptoEndpoint")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<bool>("IsSent")
                        .HasColumnType("bit");

                    b.Property<string>("MessageKey")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MsgDecryptScope")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MsgQueueEndpoint")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MsgQueueName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("RetryCount")
                        .HasColumnType("int");

                    b.Property<string>("Scheme")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SendFailReason")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ServiceInstanceId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SingDataKey")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Version")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("WasAcknowledge")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.ToTable("ExternalEvents");
                });

            modelBuilder.Entity("Sample.Sdk.EntityModel.TransactionEntity", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.ToTable("Transactions");
                });
#pragma warning restore 612, 618
        }
    }
}
