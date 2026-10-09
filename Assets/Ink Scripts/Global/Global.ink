EXTERNAL CheckState(stateKey)
EXTERNAL SetState(stateKey, stateValue)

VAR npc_location = "default"

// ==========================================
// HÀM DỰ PHÒNG (FALLBACK) CHO INKY EDITOR
// Nếu chạy trong Unity, C# sẽ thay thế các hàm này.
// Nếu chạy test trong Inky, nó sẽ dùng các hàm giả này để không bị văng lỗi.
// ==========================================

=== function CheckState(stateKey) ===
// Trả về mặc định là false khi test chay
~ return false 

=== function SetState(stateKey, stateValue) ===
// Không làm gì cả, chỉ để tránh lỗi
~ return