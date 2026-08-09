using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Story 003 AC-3: FoundationBootstrap's reset sequence must stay an ordered
/// subsequence of ADR-0001's documented dependency order. Deliberately brittle:
/// when a service epic adds its line to the sequence, ExpectedActiveOrder below
/// MUST be updated in the same change or these tests fail.
/// </summary>
public class FoundationBootstrapOrderTest
{
    // ADR-0001 Decision, "Reset ordering" code block — the documented full order.
    private static readonly string[] DocumentedOrder =
    {
        "InteractableRegistry",
        "IsikVolumeDurumSistemi",
        "GeceOturumDurumu",
        "AnlatiDurumIpucuTakibi",
        "AdaptifSesSistemi",
        "DiyalogAnlatiIcerigi",
        "ElevatorSystem",
        "GorevTasimaDongusu",
        "SahneKesmeliAnlati",
    };

    // The lines that exist in FoundationBootstrap TODAY. Story 003 ships the
    // skeleton, so this is empty; each service epic extends it with its line.
    private static readonly string[] ExpectedActiveOrder = { };

    [Test]
    public void ActiveResetOrder_CurrentLines_MatchExpectedExactly()
    {
        // Arrange
        IReadOnlyList<string> active = FoundationBootstrap.ActiveResetOrder;

        // Assert — intentional brittleness (story QA edge case): a new line must
        // update this test in the same change.
        CollectionAssert.AreEqual(ExpectedActiveOrder, active.ToArray(),
            "FoundationBootstrap._resetSequence değişti — ExpectedActiveOrder'ı aynı değişiklikte güncelle (Story 003 AC-3).");
    }

    [Test]
    public void ActiveResetOrder_IsOrderedSubsequenceOfDocumentedOrder()
    {
        // Arrange
        IReadOnlyList<string> active = FoundationBootstrap.ActiveResetOrder;

        // Act — greedy subsequence scan over the documented order.
        int docIndex = 0;
        foreach (string name in active)
        {
            while (docIndex < DocumentedOrder.Length && DocumentedOrder[docIndex] != name)
            {
                docIndex++;
            }

            // Assert
            Assert.Less(docIndex, DocumentedOrder.Length,
                $"'{name}' ADR-0001'in belgeli sırasında bu konumda yok — sıra ihlali ya da bilinmeyen servis (ADR-0001 Decision, Reset ordering).");
            docIndex++;
        }
    }

    [Test]
    public void DocumentedOrder_ExcludesSeviyeSahneGecisi()
    {
        // ADR-0008 exception: SceneTransitionManager never participates in
        // FoundationBootstrap — guard the documented list against "fixing" it in.
        CollectionAssert.DoesNotContain(DocumentedOrder, "SeviyeSahneGecisi");
        CollectionAssert.DoesNotContain(DocumentedOrder, "SceneTransitionManager");
    }
}
