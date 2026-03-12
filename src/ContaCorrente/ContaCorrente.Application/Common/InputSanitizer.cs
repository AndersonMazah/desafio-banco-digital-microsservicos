using System.Text.RegularExpressions;

namespace ContaCorrente.Application.Common;

public static partial class InputSanitizer
{
    [GeneratedRegex("[^0-9]")]
    private static partial Regex NonDigitsRegex();

    public static string NormalizeCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return string.Empty;
        }
        return NonDigitsRegex().Replace(cpf.Trim(), string.Empty);
    }

    public static string NormalizeName(string? nome)
    {
        return string.IsNullOrWhiteSpace(nome) ? string.Empty : nome.Trim();
    }

    public static string NormalizeSenha(string? senha)
    {
        if (string.IsNullOrWhiteSpace(senha))
        {
            return string.Empty;
        }
        return NonDigitsRegex().Replace(senha.Trim(), string.Empty);
    }

}
