public interface IPushable
{
    // Hàm này nhận vào quãng đường (amount) cần di chuyển. 
    // Trả về khoảng cách cần di chuyển
    float TryMove(float amount);
}