import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rassapp_controlefin/features/finance/assistant_markdown.dart';

void main() {
  Future<TextSpan> render(WidgetTester tester, String input) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: SingleChildScrollView(
            child: SizedBox(width: 230, child: AssistantMarkdown(input)),
          ),
        ),
      ),
    );
    return tester
            .widget<Text>(
              find.descendant(
                of: find.byType(AssistantMarkdown),
                matching: find.byType(Text),
              ),
            )
            .textSpan!
        as TextSpan;
  }

  testWidgets('formats a bold total while preserving Brazilian currency', (
    tester,
  ) async {
    final span = await render(tester, r'**Total**: R$ 1.234,56');
    expect(span.toPlainText(), r'Total: R$ 1.234,56');
    final line = span.children!.single as TextSpan;
    final bold = line.children!.first as TextSpan;
    expect(bold.text, 'Total');
    expect(bold.style?.fontWeight, FontWeight.w700);
  });

  testWidgets('renders bullets, numbered lists and emphasis', (tester) async {
    final span = await render(tester, '- **Mercado**\n*Lazer*\n1. Transporte');
    expect(span.toPlainText(), '• Mercado\nLazer\n1. Transporte');
    final italic = (span.children![3] as TextSpan).children!.single as TextSpan;
    expect(italic.style?.fontStyle, FontStyle.italic);
  });

  testWidgets('preserves plain text, line breaks and monetary precision', (
    tester,
  ) async {
    const text = 'Seu saldo atual é R\$ 1.234,56.\nVariação: -R\$ 0,09.';
    expect((await render(tester, text)).toPlainText(), text);
  });

  testWidgets('wraps long responses and keeps external content inert', (
    tester,
  ) async {
    const line = '![imagem](https://example.invalid/x) <script>ação</script>';
    final text = List.filled(100, line).join('\n');
    expect((await render(tester, text)).toPlainText(), text);
    expect(find.byType(Image), findsNothing);
    expect(tester.takeException(), isNull);
    await tester.drag(
      find.byType(SingleChildScrollView),
      const Offset(0, -400),
    );
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
  });
}
