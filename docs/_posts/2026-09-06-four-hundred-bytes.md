---
layout: post
title: "Four hundred bytes"
description: "A 424-byte PDF that terminates a .NET process, why a catch cannot help, and the compression bomb next to it."
date: 2026-09-06
---

This file is 424 bytes. It kills a .NET process.

```
%PDF-1.7
1 0 obj << /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles 4 0 R >> >> endobj
2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj
3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >> endobj
4 0 obj << /Kids [4 0 R] >> endobj
...
```

Look at object 4. Its `/Kids` array contains a reference to object 4.

I found this in my own code, which makes it a better story than if I had found it
in somebody else's.

## Why I was walking a PDF at all

I maintain a library that reads European e-invoices. Since January 2025 every
German company has to be able to *receive* one, and one of the formats they
receive is Factur-X: an ordinary-looking PDF with the invoice also embedded
inside it as XML. Human reads the PDF, machine reads the XML, one file.

To get at the XML you look in the document catalogue for `/Names`, then
`/EmbeddedFiles`. What you find there is a PDF name tree — a structure that is
either a leaf holding a `/Names` array of key-value pairs, or a branch holding a
`/Kids` array of more nodes.

A tree. So I wrote the obvious thing:

```csharp
private static void CollectNameTree(PdfDictionary node, Dictionary<string, byte[]> attachments)
{
    var names = node.Elements.GetArray("/Names");
    if (names is not null)
    {
        // ... pull out the attachments
    }

    var kids = node.Elements.GetArray("/Kids");
    if (kids is null) return;

    for (var index = 0; index < kids.Elements.Count; index++)
    {
        var kid = kids.Elements.GetDictionary(index);
        if (kid is not null) CollectNameTree(kid, attachments);
    }
}
```

I was pleased with this. It handles both shapes, it is short, and it reads like
the specification.

It is also a straight line to a dead process, because nothing in a PDF file
prevents object 4 from listing itself as its own child. The format is a graph of
numbered objects that reference each other. Nobody validates that the subgraph
you happen to be treating as a tree is actually a tree. The producer of the file
decides what is in it, and in my case the producer is whoever emailed my user an
invoice.

```
Stack overflow.
   at Klarfakt.Reading.PdfAttachments.CollectNameTree(...)
   at Klarfakt.Reading.PdfAttachments.CollectNameTree(...)
   at Klarfakt.Reading.PdfAttachments.CollectNameTree(...)
   ...
```

## The part that actually stung

I had a handler. I had written it deliberately, with a comment explaining itself:

```csharp
// A malformed PDF can fail anywhere inside the reader, and PDFsharp does not confine itself
// to PdfReaderException — a truncated file raises ArgumentOutOfRangeException. Callers get
// one exception type for "this file is not usable" rather than whatever surfaced.
catch (Exception exception) when (exception is not (UnsupportedDocumentException or OutOfMemoryException))
```

It does nothing here. Since .NET 2.0, a `StackOverflowException` cannot be caught.
The runtime does not raise it as an exception you may handle; it terminates the
process. Not the request, not the thread — the process. In a container handling a
queue of inbound invoices, one crafted attachment takes down the worker and
everything else it was in the middle of.

Every defensive habit I have was pointing at the wrong thing. I was thinking about
what happens if the *parser* fails. The parser was fine. It handed me a perfectly
well-formed object graph and I walked off the end of the stack myself.

The fix is not clever:

```csharp
var pending = new Stack<PdfDictionary>();
var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
var visited = 0;

pending.Push(root);

while (pending.Count > 0)
{
    if (++visited > limits.MaxPdfNameTreeNodes) throw new UnsupportedDocumentException(...);

    var node = pending.Pop();
    if (!seen.Add(node)) continue;

    // ... same body as before, pushing kids instead of recursing
}
```

Iterative, so there is no stack to overflow. A visited set, so a cycle terminates.
And a node cap, because a visited set alone still happily walks a million-node
chain that isn't a cycle at all, just long. Both guards earn their place; I tested
each with its own hand-built file.

## Then I found the second one

Feeling thorough, I went looking for the same *class* of problem elsewhere, and
found it about ten metres away.

The library has a configurable cap on attachment size. Sensible. Here is how it
was applied:

```csharp
var bytes = content?.Stream?.UnfilteredValue;

if (bytes.LongLength > limits.MaxAttachmentBytes)
{
    throw new UnsupportedDocumentException($"The attachment is {bytes.LongLength:N0} bytes...");
}
```

`UnfilteredValue` is PDFsharp's "give me the decompressed bytes" property. So the
check reads: allocate the entire attachment, then decide whether we were willing
to allocate it.

Embedded files in a PDF are usually Flate-compressed. Deflate tops out around
1000:1 on repetitive input. I generated a 261 KB PDF whose attachment expands to
256 MB, and the library dutifully allocated all 256 MB before refusing it:

```
The attachment 'factur-x.xml' is 268,435,456 bytes, over the 33,554,432 byte limit.
```

It knows the exact size. That is how you can tell it paid for it.

Scaling up, a 2.6 MB file produced a 2.5 GB attachment and died differently —
`IOException: Stream was too long`, because .NET refuses to grow a `MemoryStream`
past about 2 GB. Which is a kind of accidental safety net, if your definition of
safe includes a two-gigabyte allocation on a machine that may only have four.

The fix was to stop using the convenient property and inflate it myself, a block
at a time, checking as I go:

```csharp
while ((read = inflater.Read(chunk, 0, chunk.Length)) > 0)
{
    if (buffer.Length + read > limits.MaxAttachmentBytes) throw TooLarge(...);
    buffer.Write(chunk, 0, read);
}
```

Both bomb files now stop at 33.5 MB. They also finish *faster* than before —
1.2 seconds instead of 5.5 — because nothing large is ever materialised. That is
usually a sign the earlier version was doing something silly.

## The caveat that was not a caveat

I left one thing in and called it a deliberate trade-off. The bounded path only
handled plain Flate with *no decode parameters*; anything more exotic — a
predictor, a chain of filters — still went through PDFsharp with the check
afterwards. Refusing an unusual filter, I reasoned, would mean losing a readable
invoice in order to save memory, and that is the wrong trade for a compliance
tool.

Someone reviewing the code pointed out what I had actually written.

```
/DecodeParms << /Predictor 1 >>
```

Predictor 1 means *no prediction*. The bytes are ordinary Flate. Adding that
dictionary changes nothing whatsoever about the data and everything about which
code path reads it — and the sender writes the dictionary. So the sender decides
whether the size limit applies.

Measured against the old code: a 64 MB payload, against a **1 KB** limit,
allocated 134,568,904 bytes.

Predictor 1 and an explicit null are recognised as plain Flate now, and a filter
the library will not decode is refused rather than expanded. With no filter at
all there is nothing to expand, so an uncompressed attachment still reads.

The trade I thought I was making was not the trade I was making.

## What I took from it

**A limit you check afterwards is not a limit.** It is a report. If the expensive
thing has already happened by the time you test the condition, you have written
an assertion, not a guard.

**Know which exceptions your runtime will not let you catch.** `StackOverflowException`
is the obvious one and it is genuinely different from everything else in .NET: no
`catch`, no `finally`, no `AppDomain.UnhandledException`. If untrusted input can
reach recursive code, the recursion is the bug, not the missing handler.

**"It's a tree" is an assumption about the producer, not the format.** PDF, XML
with entity references, JSON with `$ref`, protobuf with recursive messages — in
every case the shape you are relying on is a convention the sender can decline to
follow. My name-tree walker was correct for every file produced by software that
wasn't trying to hurt me.

**A limit the sender can route around is not a limit either.** This is the first
lesson again, which is why it stings. I moved the check before the allocation,
called it fixed, and left a second door open by letting the document's own
metadata choose which code path did the measuring. The question is not only
whether something is checked. It is who decides whether the check runs.

**Test the guard, not the happy path.** All of these were found by sitting down
and writing files specifically designed to break my own code: a self-referencing
node, a two-node cycle, a node that is its own grandchild, a 6,000-node chain,
two compression bombs, and a bomb wearing a no-op predictor. All of them are in
the test suite now. None would have appeared in any corpus of real invoices,
which is exactly why 518 files of real corpus told me nothing about any of them.

The library is [Klarfakt](https://github.com/yivo-0/Klarfakt), if you want to
see the fixes in context. But none of these mistakes are specific to PDFs or to
e-invoicing, which is why I wrote this instead of a release note.
