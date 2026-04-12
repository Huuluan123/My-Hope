public interface IPushable
{
    // Hàm này nhận vào quãng đường (amount) cần di chuyển. 
    // Trả về TRUE nếu đẩy/kéo thành công, FALSE nếu cái hộp bị kẹt vào tường.
    bool TryMove(float amount);
}