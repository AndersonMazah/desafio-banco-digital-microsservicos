namespace ContaCorrente.Application.Common;

public static class Validators
{
    public static bool IsNomeValido(string nome)
    {
        return !string.IsNullOrWhiteSpace(nome) && nome.Length is >= 1 and <= 120;
    }

    public static bool IsSenhaValida(string senha)
    {
        return !string.IsNullOrWhiteSpace(senha) && senha.Length == 6 && senha.All(char.IsLetterOrDigit);
    }

    public static bool IsCpfFormatoValido(string cpf)
    {
        return cpf.Length == 11 && cpf.All(char.IsDigit);
    }

    public static bool IsTipoMovimentoValido(string tipo)
    {
        return tipo is "C" or "D";
    }

    public static bool IsContaLoginValida(string? conta)
    {
        if (string.IsNullOrWhiteSpace(conta))
        {
            return true;
        }
        return long.TryParse(conta.Trim(), out var numero) && numero >= 1;
    }

    public static bool IsCpfValido(string cpf)
    {
        if (!IsCpfFormatoValido(cpf))
        {
            return false;
        }
        if (cpf.Distinct().Count() == 1)
        {
            return false;
        }
        static int CalcularDigito(ReadOnlySpan<char> cpfSpan, int pesoInicial)
        {
            var soma = 0;
            for (var i = 0; i < cpfSpan.Length; i++)
            {
                soma += (cpfSpan[i] - '0') * (pesoInicial - i);
            }
            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
        var primeiroDigito = CalcularDigito(cpf.AsSpan(0, 9), 10);
        var segundoDigito = CalcularDigito(cpf.AsSpan(0, 10), 11);
        return cpf[9] - '0' == primeiroDigito && cpf[10] - '0' == segundoDigito;
    }
    
}
