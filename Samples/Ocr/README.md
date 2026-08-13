# OCR / 导入脱敏样例

本目录仅放**已脱敏**的短信/订单/邮件文本，供规则回归与手工对照。

- 勿提交含真实姓名、证件号、手机号、完整订单号的原图或原文。
- 单元测试可读取 `*.txt`；期望字段见各文件头注释或对应 `TicketTextExtractorTests`。

| 文件 | 来源族 |
|------|--------|
| `sms-12306.txt` | A 12306 短信 |
| `sms-zhixing.txt` | H 智行 |
| `order-detail-labeled.txt` | D 订单详情 |
| `paper-ticket-ocr.txt` | G 纸票 OCR |
| `email-html.txt` | B 购票邮件 HTML |
| `benren-chepiao-card.txt` | C 本人车票卡 |
| `share-card.txt` | E 行程分享 |
