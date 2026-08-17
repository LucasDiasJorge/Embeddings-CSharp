using Inventory.Domain.Items;

namespace Inventory.Domain.Counting;

/// <summary>
/// O veredito da contagem: o confronto entre o que o sistema esperava e o que o auditor viu.
/// </summary>
/// <param name="Confirmed">Esperados e encontrados. O sistema estava certo.</param>
/// <param name="Missing">Esperados e não encontrados. Viram <see cref="ItemStatus.Missing"/>.</param>
/// <param name="Unexpected">Encontrados sem serem esperados. O sistema estava errado sobre onde eles estavam — serão realocados para cá.</param>
public sealed record CountReconciliation(
    IReadOnlyList<ItemId> Confirmed,
    IReadOnlyList<ItemId> Missing,
    IReadOnlyList<ItemId> Unexpected)
{
    /// <summary>Uma contagem sem divergência alguma: o sistema refletia a realidade.</summary>
    public bool IsClean => Missing.Count == 0 && Unexpected.Count == 0;

    /// <summary>
    /// Fração dos itens esperados que foram de fato encontrados (0..1).
    /// É a métrica clássica de acuracidade de inventário.
    /// </summary>
    public double Accuracy
    {
        get
        {
            var expected = Confirmed.Count + Missing.Count;
            return expected == 0 ? 1d : (double)Confirmed.Count / expected;
        }
    }
}
