using ColetaSeletiva.Domain.Identidade;
using Microsoft.AspNetCore.Identity;

namespace ColetaSeletiva.Infrastructure.Identidade;

public static class PapelSeeder
{
    public static async Task GarantirPapeisAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        ArgumentNullException.ThrowIfNull(roleManager);

        foreach (var papel in Papeis.Todos)
        {
            if (await roleManager.RoleExistsAsync(papel))
            {
                continue;
            }

            var resultado = await roleManager.CreateAsync(
                new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = papel,
                });

            if (!resultado.Succeeded)
            {
                var codigos = string.Join(", ", resultado.Errors.Select(erro => erro.Code));
                throw new InvalidOperationException(
                    $"Não foi possível criar o papel '{papel}': {codigos}.");
            }
        }
    }
}
