using ColetaSeletiva.Domain.Entidades;

namespace ColetaSeletiva.Application.Acesso.RegistroDeOrganizacao;

public sealed record RegistroDeOrganizacaoEntrada(
    string NomeOrganizacao,
    TipoOrganizacao Tipo,
    string Usuario,
    string Email,
    string Senha);
