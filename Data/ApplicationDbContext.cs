using System;
using System.Net.NetworkInformation;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using maverickApi.Models;
using Microsoft.EntityFrameworkCore;
//Este es un mensaje para el proximo programador que revise este codigo hoy 07/04/2026 se realizo este codigo se integran las funciones para migrar de base de datos, declarando las reglas de los modelos protegiendo asi que no se eliminen los registros para evitar errores en el negocio
namespace maverickApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : base(options) { }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<DetalleOrdenCompra> DetalleOrdenCompras { get; set; }
        public DbSet<DetalleVenta> DetalleVentas { get; set; }
        public DbSet<OrdenCompra> OrdenCompras { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Venta> Ventas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Debe eliminarse al Desplegar la api
            modelBuilder.Entity<Usuario>().HasData(
            new Usuario
            {
                Id = 1,
                Nombre = "Admin",
                Apellidos = "Sistema",
                Email = "admin@maverick.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123"),
                Admin = true,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            }
        );
            base.OnModelCreating(modelBuilder);
            // ===== Categoria ====
            modelBuilder.Entity<Categoria>()
                .HasMany(v => v.Productos)
                .WithOne(p => p.Categoria)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== VENTA =====

            // Venta - Usuario
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Usuario)
                .WithMany()
                .HasForeignKey(v => v.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Venta - Cliente
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Cliente)
                .WithMany()
                .HasForeignKey(v => v.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Venta - DetalleVenta (¡CORREGIDO!)
            modelBuilder.Entity<Venta>()
                .HasMany(v => v.Detalles)
                .WithOne(d => d.Venta)
                .HasForeignKey(d => d.VentaId)
                .OnDelete(DeleteBehavior.Cascade);

            // DetalleVenta - Producto
            modelBuilder.Entity<DetalleVenta>()
                .HasOne(d => d.Producto)
                .WithMany()
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== ORDEN COMPRA =====

            modelBuilder.Entity<OrdenCompra>()
                .HasOne<Proveedor>()
                .WithMany()
                .HasForeignKey(oc => oc.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrdenCompra>()
                .HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(oc => oc.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DetalleOrdenCompra>()
                .HasOne<OrdenCompra>()
                .WithMany()
                .HasForeignKey(doc => doc.OrdenCompraId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DetalleOrdenCompra>()
                .HasOne<Producto>()
                .WithMany()
                .HasForeignKey(doc => doc.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==== Proveedor ====
            modelBuilder.Entity<Proveedor>()
                .HasMany(p => p.Productos)
                .WithOne(pd => pd.Proveedor)
                .HasForeignKey(pd => pd.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);


            // ===== ÍNDICES UNIQUE =====
            modelBuilder.Entity<Producto>()
                .HasIndex(p => p.Sku)
                .IsUnique();

            modelBuilder.Entity<Producto>()
                .HasIndex(p => p.Nombre);

            modelBuilder.Entity<Proveedor>()
                .HasIndex(p => p.Rfc)
                .IsUnique();

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.Rfc)
                .IsUnique();
        }
    }
}