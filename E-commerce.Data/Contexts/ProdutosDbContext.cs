using Dunder_Store.E_commerce.Business.Entities;
using Dunder_Store.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dunder_Store.Database
{
    public partial class ProdutosDbContext : DbContext
    {
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<PedidoProduto> PedidoProdutos { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Cupom> Cupons { get; set; }
        public DbSet<PrecoRegiao> PrecoRegiao { get; set; }

        public ProdutosDbContext(DbContextOptions<ProdutosDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ==========================
            // ENTIDADE: PedidoProduto (N:N)
            // ==========================
            modelBuilder.Entity<PedidoProduto>()
                .HasKey(pp => new { pp.PedidoId, pp.ProdutoId }); // Chave composta

            modelBuilder.Entity<PedidoProduto>()
                .HasOne(pp => pp.Pedido)
                .WithMany(p => p.PedidoProdutos)
                .HasForeignKey(pp => pp.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PedidoProduto>()
                .HasOne(pp => pp.Produto)
                .WithMany(p => p.PedidoProdutos)
                .HasForeignKey(pp => pp.ProdutoId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================
            // ENTIDADE: Produto
            // ==========================
            modelBuilder.Entity<Produto>()
                .HasMany(p => p.Variacoes)
                .WithOne(v => v.ProdutoPai)
                .HasForeignKey(v => v.ProdutoPaiId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Produto>()
                .HasOne(p => p.Categoria)
                .WithMany(c => c.Produtos)
                .HasForeignKey(p => p.CategoriaId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // ==========================
            // ENTIDADE: Categoria
            // ==========================
            modelBuilder.Entity<Categoria>()
                .HasMany(c => c.Subcategorias)
                .WithOne(c => c.CategoriaPai)
                .HasForeignKey(c => c.CategoriaPaiId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================
            // ENTIDADE: Pedido
            // ==========================
            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Cliente)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================
            // SEED: PrecoRegiao
            // ==========================
            modelBuilder.Entity<PrecoRegiao>().HasData(
                new PrecoRegiao { Id = 1, Regiao = "Norte", PrecoBase = 25.0m },
                new PrecoRegiao { Id = 2, Regiao = "Nordeste", PrecoBase = 20.0m },
                new PrecoRegiao { Id = 3, Regiao = "Centro-Oeste", PrecoBase = 18.0m },
                new PrecoRegiao { Id = 4, Regiao = "Sudeste", PrecoBase = 12.0m },
                new PrecoRegiao { Id = 5, Regiao = "Sul", PrecoBase = 15.0m }
            );

            base.OnModelCreating(modelBuilder);
        }
    }
}
