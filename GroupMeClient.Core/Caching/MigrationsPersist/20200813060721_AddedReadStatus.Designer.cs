﻿// <auto-generated />
using GroupMeClient.Core.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GroupMeClient.Core.Caching.MigrationsPersist
{
    [DbContext(typeof(PersistManager.PersistContext))]
    [Migration("20200813060721_AddedReadStatus")]
    partial class AddedReadStatus
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.6");

            modelBuilder.Entity("GroupMeClient.Core.Caching.Models.GroupIndexStatus", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("LastIndexedId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("IndexStatus");
                });

            modelBuilder.Entity("GroupMeClient.Core.Caching.Models.GroupOrChatState", b =>
                {
                    b.Property<string>("GroupOrChatId")
                        .HasColumnType("TEXT");

                    b.Property<int>("LastTotalMessageCount")
                        .HasColumnType("INTEGER");

                    b.HasKey("GroupOrChatId");

                    b.ToTable("GroupChatStates");
                });

            modelBuilder.Entity("GroupMeClient.Core.Caching.Models.HiddenMessage", b =>
                {
                    b.Property<string>("MessageId")
                        .HasColumnType("TEXT");

                    b.Property<string>("ConversationId")
                        .HasColumnType("TEXT");

                    b.HasKey("MessageId");

                    b.HasIndex("ConversationId");

                    b.ToTable("HiddenMessages");
                });

            modelBuilder.Entity("GroupMeClient.Core.Caching.Models.StarredMessage", b =>
                {
                    b.Property<string>("MessageId")
                        .HasColumnType("TEXT");

                    b.Property<string>("ConversationId")
                        .HasColumnType("TEXT");

                    b.HasKey("MessageId");

                    b.HasIndex("ConversationId");

                    b.ToTable("StarredMessages");
                });
#pragma warning restore 612, 618
        }
    }
}
