using Amazon.S3.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using QLN.Common.DTO_s.Payments;
using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QLN.Common.Infrastructure.QLDbContext
{
    public class QLPaymentsContext : DbContext
    {
        public DbSet<D365PaymentLogsEntity> D365PaymentLogs { get; set; }
        public DbSet<D365RequestsLogsEntity> D365RequestsLogs { get; set; }
        public DbSet<PaymentEntity> Payments { get; set; }
        public DbSet<D365LookupEntity> D365Lookups { get; set; }

        public QLPaymentsContext(DbContextOptions<QLPaymentsContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var jsonOpts = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var e = modelBuilder.Entity<PaymentEntity>();

            var productsConverter = new ValueConverter<List<ProductDetails>, string>(
                v => JsonSerializer.Serialize(v ?? new List<ProductDetails>(), jsonOpts),
                v => SafeDeserializeProducts(v, jsonOpts)
            );

            var productsComparer = new ValueComparer<List<ProductDetails>>(
                (a, b) => JsonSerializer.Serialize(a ?? new(), jsonOpts) ==
                          JsonSerializer.Serialize(b ?? new(), jsonOpts),
                v => JsonSerializer.Serialize(v ?? new(), jsonOpts).GetHashCode(),
                v => v == null ? new List<ProductDetails>() : v.ToList()
            );

            e.Property(p => p.Products)
                .HasConversion(productsConverter)
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'[]'::jsonb")
                .Metadata.SetValueComparer(productsComparer);

            e.Property(p => p.UserAddonIds)
                .HasColumnType("uuid[]")
                .HasDefaultValueSql("'{}'::uuid[]");
        }

        
        private static List<ProductDetails> SafeDeserializeProducts(string v, JsonSerializerOptions opts)
        {
            if (string.IsNullOrWhiteSpace(v)) return new List<ProductDetails>();

            try
            {
                return JsonSerializer.Deserialize<List<ProductDetails>>(v, opts) ?? new List<ProductDetails>();
            }
            catch
            {
                
                return new List<ProductDetails>();
            }
        }
    }
}
