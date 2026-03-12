namespace Transferencia.Application.Common;

public static class Validators
{
    public static bool IsGuidValido(Guid value) => value != Guid.Empty;
    public static bool IsValorMonetarioValido(decimal value) => value > 0;
}
