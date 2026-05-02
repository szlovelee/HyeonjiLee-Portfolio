public interface IInteractable
{
    public bool CanInteractWithPlayer { get; set; }
    public bool IsPlaceable { get; set; }

    public void Interaction();

    public void PickUp();

    public void PutDown();

    public void SetPlayer(Player player);
}
