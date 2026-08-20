using NUnit.Framework;
using Jeomseon.Unity.GridTileSystem;
using Jeomseon.Unity.GridTileSystem.Services;

namespace Jeomseon.HexGrid.Tests
{
    public sealed class HexGridSelectionStateTests
    {
        [Test]
        public void UpdateHover_OnNewTile_InvokesTileListenerAndServiceEventExactlyOnce()
        {
            global::Jeomseon.Unity.GridTileSystem.HexGrid hex = new(0, 0);
            int tileListenerCalls = 0;
            int serviceEventCalls = 0;
            hex.OnEnterTile += _ => tileListenerCalls++;

            HexGridSelectionState selection = new();
            selection.Entered += _ => serviceEventCalls++;

            selection.UpdateHover(hex);

            Assert.That(tileListenerCalls, Is.EqualTo(1));
            Assert.That(serviceEventCalls, Is.EqualTo(1));
        }

        [Test]
        public void UpdateHover_MovingToAnotherTile_FiresExitOnPreviousAndEnterOnNextExactlyOnceEach()
        {
            global::Jeomseon.Unity.GridTileSystem.HexGrid first = new(0, 0);
            global::Jeomseon.Unity.GridTileSystem.HexGrid second = new(1, 0);
            int firstExitCalls = 0;
            int secondEnterCalls = 0;
            first.OnExitTile += _ => firstExitCalls++;
            second.OnEnterTile += _ => secondEnterCalls++;

            HexGridSelectionState selection = new();
            int exitedEventCalls = 0;
            int enteredEventCalls = 0;
            selection.Exited += _ => exitedEventCalls++;
            selection.Entered += _ => enteredEventCalls++;

            selection.UpdateHover(first);
            selection.UpdateHover(second);

            Assert.That(firstExitCalls, Is.EqualTo(1));
            Assert.That(secondEnterCalls, Is.EqualTo(1));
            Assert.That(exitedEventCalls, Is.EqualTo(1));
            Assert.That(enteredEventCalls, Is.EqualTo(2));
        }

        [Test]
        public void UpdateHover_WithSameTileAgain_DoesNotRefireEvents()
        {
            global::Jeomseon.Unity.GridTileSystem.HexGrid hex = new(0, 0);
            int enterCalls = 0;
            hex.OnEnterTile += _ => enterCalls++;

            HexGridSelectionState selection = new();
            selection.UpdateHover(hex);
            selection.UpdateHover(hex);

            Assert.That(enterCalls, Is.EqualTo(1));
        }

        [Test]
        public void UpdateHover_WithNullAfterHover_FiresExitExactlyOnce()
        {
            global::Jeomseon.Unity.GridTileSystem.HexGrid hex = new(0, 0);
            int exitCalls = 0;
            hex.OnExitTile += _ => exitCalls++;

            HexGridSelectionState selection = new();
            selection.UpdateHover(hex);
            selection.UpdateHover(null);

            Assert.That(exitCalls, Is.EqualTo(1));
        }

        [Test]
        public void NotifyMouseDown_InvokesTileListenerAndServiceEventExactlyOnce()
        {
            global::Jeomseon.Unity.GridTileSystem.HexGrid hex = new(0, 0);
            int tileListenerCalls = 0;
            int serviceEventCalls = 0;
            hex.OnMouseDownTile += _ => tileListenerCalls++;

            HexGridSelectionState selection = new();
            selection.MouseDown += _ => serviceEventCalls++;

            selection.NotifyMouseDown(hex);

            Assert.That(tileListenerCalls, Is.EqualTo(1));
            Assert.That(serviceEventCalls, Is.EqualTo(1));
        }

        [Test]
        public void NotifyMouseUp_InvokesTileListenerAndServiceEventExactlyOnce()
        {
            global::Jeomseon.Unity.GridTileSystem.HexGrid hex = new(0, 0);
            int tileListenerCalls = 0;
            int serviceEventCalls = 0;
            hex.OnMouseUpTile += _ => tileListenerCalls++;

            HexGridSelectionState selection = new();
            selection.MouseUp += _ => serviceEventCalls++;

            selection.NotifyMouseUp(hex);

            Assert.That(tileListenerCalls, Is.EqualTo(1));
            Assert.That(serviceEventCalls, Is.EqualTo(1));
        }

        [Test]
        public void NotifyMouseDown_WithNullTile_DoesNothing()
        {
            HexGridSelectionState selection = new();
            int calls = 0;
            selection.MouseDown += _ => calls++;

            Assert.DoesNotThrow(() => selection.NotifyMouseDown(null));
            Assert.That(calls, Is.EqualTo(0));
        }
    }
}
