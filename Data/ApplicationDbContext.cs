using System;
using System.Net.NetworkInformation;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using maverickApi.Models;
using maverickApi.Services;
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
            //Insertar usuarios
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
                FechaCreacion = DateTime.Now
            },
            new Usuario
            {
                Id = 2,
                Nombre = "Ventas",
                Apellidos = "Sistema",
                Email = "ventasr@maverick.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Ventas123"),
                Admin = false,
                Activo = true,
                FechaCreacion = DateTime.Now
            });


            //Insertar categorias

            modelBuilder.Entity<Categoria>().HasData(
                new Categoria
                {
                    Id = 1,
                    Nombre = "Motor",
                    Descripcion = "Material para motor",
                    Productos = new List<Producto>()

                },
                new Categoria
                {
                    Id = 2,
                    Nombre = "Transmision",
                    Descripcion = "Material para transmision",
                    Productos = new List<Producto>()

                },
                new Categoria
                {
                    Id = 3,
                    Nombre = "Suspension",
                    Descripcion = "Material para suspension",
                    Productos = new List<Producto>()

                },
                new Categoria
                {
                    Id = 4,
                    Nombre = "Embrague",
                    Descripcion = "Material para Embrague",
                    Productos = new List<Producto>()

                },
                 new Categoria
                 {
                     Id = 5,
                     Nombre = "Hidraulica",
                     Descripcion = "Material de Hidraulica",
                     Productos = new List<Producto>()

                 });
            // //Insertar proveedores
            modelBuilder.Entity<Proveedor>().HasData(
                new Proveedor
                {
                    Id = 1,
                    Nombre = "Autozone SA de CV",
                    Email = "autozone@autozone.com",
                    Telefono = "5506667812",
                    Direccion = "Col Miguel Hidalgo, Ciudad de Mexico.",
                    Rfc = "A12345678",
                    Activo = true,
                    Productos = new List<Producto>(),
                    FechaCreacion = DateTime.Now

                },
                new Proveedor
                {
                    Id = 2,
                    Nombre = "Hidraulica Diviro SA de CV",
                    Email = "diviro@hotmail.com",
                    Telefono = "6671020304",
                    Direccion = "Blvd Benjamin Hill Culiacan Sin.",
                    Rfc = "H1234567890",
                    Activo = true,
                    Productos = new List<Producto>(),
                    FechaCreacion = DateTime.Now

                },
                new Proveedor
                {
                    Id = 3,
                    Nombre = "Mantenimientos y Servicios Daguza SA de CV",
                    Email = "daguza@hotmail.com",
                    Telefono = "6673121914",
                    Direccion = "Blvd Enrique Cabrera Culiacan Sin.",
                    Rfc = "D12345",
                    Activo = false,
                    Productos = new List<Producto>(),
                    FechaCreacion = DateTime.Now

                });
            // //Insertar productos
            modelBuilder.Entity<Producto>().HasData(
                new Producto
                {
                    Id = 1,
                    Sku = "bnd-1234",
                    CodigoBarras = "1086543",
                    Stock = 100,
                    Nombre = "Banda 10 pulgadas",
                    Descripcion = "Banda de motor de 10 pulgadas.",
                    PrecioCompra = 450,
                    PrecioVenta = 905,
                    Marca = "Autozone",
                    Modelo = "hrmg1224",
                    CategoriaId = 1,
                    ProveedorId = 1,
                    Activo = true,
                    FechaCreacion = DateTime.Now

                },
                new Producto
                {
                    Id = 2,
                    Sku = "sdt-2333",
                    CodigoBarras = "85728750",
                    Stock = 100,
                    Nombre = "Soporte de transmision",
                    Descripcion = "Soporte de transmision para Toyota Corolla 2000 - 2013 ",
                    PrecioCompra = 250,
                    PrecioVenta = 750,
                    Marca = "Autozone",
                    Modelo = "sop3456",
                    CategoriaId = 2,
                    ProveedorId = 1,
                    Activo = true,
                    FechaCreacion = DateTime.Now

                },
                new Producto
                {
                    Id = 3,
                    Sku = "hit-37698",
                    CodigoBarras = "254324378",
                    Stock = 100,
                    Nombre = "Horquilla inferior",
                    Descripcion = "Horquilla inferior trasera de fiat 100",
                    PrecioCompra = 344,
                    PrecioVenta = 1200,
                    Marca = "Autozone",
                    Modelo = "horin1243254",
                    CategoriaId = 3,
                    ProveedorId = 1,
                    Activo = true,
                    FechaCreacion = DateTime.Now

                },
                new Producto
                {
                    Id = 4,
                    Sku = "pet-32434",
                    CodigoBarras = "38748543",
                    Stock = 100,
                    Nombre = "Piston elevador trasero",
                    Descripcion = "Piston eleveador trasero",
                    PrecioCompra = 12500,
                    PrecioVenta = 23400,
                    Marca = "Diviro",
                    Modelo = "piset567",
                    CategoriaId = 5,
                    ProveedorId = 2,
                    Activo = true,
                    FechaCreacion = DateTime.Now

                },
                new Producto
                {
                    Id = 5,
                    Sku = "lh-3435",
                    CodigoBarras = "56765643",
                    Stock = 100,
                    Nombre = "Linea de hidarulico",
                    Descripcion = "Linea del hidarulico",
                    PrecioCompra = 1255,
                    PrecioVenta = 20300,
                    Marca = "Diviro",
                    Modelo = "lihid213",
                    CategoriaId = 5,
                    ProveedorId = 2,
                    Activo = true,
                    FechaCreacion = DateTime.Now

                },
                new Producto
                {
                    Id = 6,
                    Sku = "",
                    CodigoBarras = "325783",
                    Stock = 100,
                    Nombre = "Rin 14 pulgadas",
                    Descripcion = "Rin de fibra de carbono de 14 pulgadas",
                    PrecioCompra = 1230,
                    PrecioVenta = 2500,
                    Marca = "Daguza",
                    Modelo = "Rifi69",
                    CategoriaId = 5,
                    ProveedorId = 3,
                    Activo = false,
                    FechaCreacion = DateTime.Now

                }
            );
            modelBuilder.Entity<Cliente>().HasData(
                new Cliente
                {
                    Id = 1,
                    Nombre = "Taller el giro",
                    Rfc = "24sa12e",
                    Telefono = "456734",
                    Email = "hiro@hotmail.com",
                    Activo = true,
                    FechaCreacion = DateTime.Now
                },
                new Cliente
                {
                    Id = 2,
                    Nombre = "Michael Jackson",
                    Rfc = "345s32r",
                    Telefono = "56732245",
                    Email = "michael@gmail.com",
                    Activo = true,
                    FechaCreacion = DateTime.Now
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
                .HasOne(oc => oc.Proveedor)
                .WithMany()
                .HasForeignKey(oc => oc.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrdenCompra>()
                .HasOne(oc => oc.Usuario)
                .WithMany()
                .HasForeignKey(oc => oc.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DetalleOrdenCompra>()
                .HasOne(d => d.OrdenCompra)
                .WithMany(doc => doc.Detalles)
                .HasForeignKey(doc => doc.OrdenCompraId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DetalleOrdenCompra>()
                .HasOne(d => d.Producto)
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