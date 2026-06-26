INCLUDE ../Global/Global.ink

~ temp Anna_RescueScene = CheckState("Anna_RescueScene")

->Start

=== Start ===
{ Anna_RescueScene:
    -> AfterBeingSaved
- else:
    -> Anna_Town
}

== AfterBeingSaved ==
# speaker: Anna
Cản ơn bạn đã cứu mình!
# speaker: Hope
Nhiệm vụ của mình là cứu và yêu thương con người. Không có gì to tác đâu!
# speaker: Anna
Bạn trông tốt bụng và còn là một robot nữa. Bạn đến từ đâu thế?
# speaker: Hope
Mình không biết nữa. Ký ức của mình trống trơn!
# speaker: Anna
Vậy thì tên bạn là gì vậy?
# speaker: Hope
Mình là Robot. Nhiệm vụ của mình là cứu và yêu thương con người. Đó là tất cả những gì trong ký ức của mình.
# speaker: Anna
Mình là Anna. Rất vui được gặp bạn, Robot!
Có một thị trấn không xa đây. Đó là nơi mình sống. Có rất nhiều người ở đó. Bạn có muốn đến cùng mình không?
# speaker: Hope
Được thôi!
# speaker: System
Mở khoá khu vực mới: Thị trấn [town]
-> END

== Anna_Town ===
#speaker: Anna
Chào mừng đến với thị trấn của mình! Mình nghĩ bạn sẽ thích nó đấy!
Tại sao không thử dạo một vòng quanh thị trấn nhỉ? Nó sẽ khá vui đấy!
-> END