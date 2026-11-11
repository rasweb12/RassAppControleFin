import 'package:flutter/material.dart';

/// Displays the text-only Markdown subset used in assistant responses.
/// Links, images and HTML stay inert. The surrounding chat owns scrolling.
class AssistantMarkdown extends StatelessWidget {
  const AssistantMarkdown(this.text, {super.key});

  final String text;

  static final _bullet = RegExp(r'^([ \t]*)[-+*][ \t]+(?=\S)');
  static final _heading = RegExp(r'^#{1,6}[ \t]+(?=\S)');
  static final _inline = RegExp(
    r'(\\[\\*_`])|(\*\*([^*]+)\*\*|__([^_]+)__)'
    r'|(\*([^*]+)\*|(?<!\w)_([^_]+)_(?!\w))|(`([^`]+)`)',
  );

  @override
  Widget build(BuildContext context) {
    final spans = <TextSpan>[];
    final lines = text.split('\n');
    for (var index = 0; index < lines.length; index++) {
      if (index > 0) spans.add(const TextSpan(text: '\n'));
      final line = lines[index];
      final bullet = _bullet.firstMatch(line);
      final heading = _heading.firstMatch(line);
      if (bullet != null) {
        spans.add(TextSpan(text: '${bullet.group(1)}• '));
      }
      final content = line.substring(bullet?.end ?? heading?.end ?? 0);
      spans.add(
        TextSpan(
          style: heading == null
              ? null
              : const TextStyle(fontWeight: FontWeight.w700),
          children: _inlineSpans(content),
        ),
      );
    }
    return Text.rich(TextSpan(children: spans));
  }

  static List<TextSpan> _inlineSpans(String line) {
    final spans = <TextSpan>[];
    var offset = 0;
    for (final match in _inline.allMatches(line)) {
      if (match.start > offset) {
        spans.add(TextSpan(text: line.substring(offset, match.start)));
      }
      final escaped = match.group(1);
      final bold = match.group(3) ?? match.group(4);
      final italic = match.group(6) ?? match.group(7);
      if (escaped != null) {
        spans.add(TextSpan(text: escaped.substring(1)));
      } else if (bold != null) {
        spans.add(
          TextSpan(
            text: bold,
            style: const TextStyle(fontWeight: FontWeight.w700),
          ),
        );
      } else if (italic != null) {
        spans.add(
          TextSpan(
            text: italic,
            style: const TextStyle(fontStyle: FontStyle.italic),
          ),
        );
      } else {
        spans.add(
          TextSpan(
            text: match.group(9),
            style: const TextStyle(fontFamily: 'monospace'),
          ),
        );
      }
      offset = match.end;
    }
    if (offset < line.length) spans.add(TextSpan(text: line.substring(offset)));
    return spans;
  }
}
