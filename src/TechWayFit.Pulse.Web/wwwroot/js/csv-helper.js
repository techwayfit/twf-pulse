/**
 * CsvHelper — reusable CSV / TSV / plain-text paste normalizer + file importer.
 *
 * --- Paste API ---
 *   CsvHelper.normalize(rawText)                    → one-item-per-line string (first column)
 *   CsvHelper.parseFirstColumn(rawText)             → string[]
 *   CsvHelper.wireTextareaPaste('id', options?)     → bind smart paste to textarea
 *
 * --- File Import API ---
 *   CsvHelper.readFile(file, maxRows?)
 *     → Promise<{ headers: string[], rows: string[][], isTsv: boolean }>
 *     headers = first row values used as column labels
 *     rows    = up to maxRows (default 200) data rows after the header
 *
 *   CsvHelper.bindFileUpload({
 *     fileInputId,       // id of <input type="file">
 *     textareaId,        // id of <textarea> to populate
 *     columnPickerId,    // id of column-picker container div
 *     columnSelectId,    // id of <select> inside the picker
 *     maxRows?,          // default 200
 *   })
 *   → wires the full import flow: file → parse → (multi-col → picker) → textarea
 */
const CsvHelper = (function () {
    'use strict';

    /**
     * Parses a single CSV line, returning all field values.
     * Handles double-quoted fields that may contain commas or newlines.
     * @param {string} line
     * @returns {string[]}
     */
    function parseCsvLine(line) {
        const fields = [];
        let i = 0;
        while (i < line.length) {
            if (line[i] === '"') {
                // Quoted field
                let field = '';
                i++; // skip opening quote
                while (i < line.length) {
                    if (line[i] === '"' && line[i + 1] === '"') {
                        field += '"';
                        i += 2;
                    } else if (line[i] === '"') {
                        i++; // skip closing quote
                        break;
                    } else {
                        field += line[i++];
                    }
                }
                fields.push(field);
                // skip comma after field
                if (line[i] === ',') i++;
            } else {
                // Unquoted field — read until comma
                const start = i;
                while (i < line.length && line[i] !== ',') i++;
                fields.push(line.slice(start, i));
                if (line[i] === ',') i++;
            }
        }
        return fields;
    }

    /**
     * Extracts the first column from each row of text.
     * Handles: TSV (Excel multi-column), CSV (comma-separated), and plain newlines.
     * @param {string} text  Raw clipboard text
     * @returns {string[]}   Array of trimmed, non-empty first-column values
     */
    function parseFirstColumn(text) {
        if (!text) return [];

        const normalized = text.replace(/\r\n/g, '\n').replace(/\r/g, '\n');
        const lines = normalized.split('\n');
        const result = [];

        for (const line of lines) {
            if (!line.trim()) continue;

            let value;
            if (line.includes('\t')) {
                // TSV / Excel multi-column paste — take everything before the first tab
                value = line.split('\t')[0];
            } else if (line.includes(',')) {
                // CSV — parse properly to handle quoted fields
                const fields = parseCsvLine(line);
                value = fields[0] ?? line;
            } else {
                // Plain text — use the whole line
                value = line;
            }

            // Strip surrounding whitespace and paired quotes left from simple splits
            value = value.trim().replace(/^(["'])(.*)\1$/, '$2').trim();

            if (value) result.push(value);
        }

        return result;
    }

    /**
     * Normalizes pasted text to one-item-per-line format (first column only).
     * @param {string} text  Raw clipboard text
     * @returns {string}     Cleaned text with one item per line
     */
    function normalize(text) {
        return parseFirstColumn(text).join('\n');
    }

    /**
     * Detects whether pasted text would benefit from normalization
     * (i.e. it contains tab or CSV separators).
     * @param {string} text
     * @returns {boolean}
     */
    function needsNormalization(text) {
        return /\t/.test(text) || /(?:^|,)"/.test(text) || /^[^,\n]+,[^,\n]+/m.test(text);
    }

    /**
     * Attaches a smart paste handler to a textarea element.
     * Idempotent — calling this multiple times on the same element is safe.
     *
     * @param {string|HTMLTextAreaElement} target  Element id or element reference
     * @param {{ onPaste?: (normalizedText: string) => void }} [options]
     */
    function wireTextareaPaste(target, options) {
        const el = typeof target === 'string' ? document.getElementById(target) : target;
        if (!el || el.dataset.csvPasteBound) return;
        el.dataset.csvPasteBound = '1';

        el.addEventListener('paste', function (e) {
            const text = (e.clipboardData || window.clipboardData).getData('text');
            if (!text || !needsNormalization(text)) return; // let default paste handle plain text

            const normalizedText = normalize(text);
            if (normalizedText === text) return; // no change — let default handle it

            e.preventDefault();

            // Insert normalized text at the current cursor / selection position
            const start = el.selectionStart;
            const end = el.selectionEnd;
            const before = el.value.slice(0, start);
            const after = el.value.slice(end);

            // Add a newline separator when inserting mid-content
            const prefix = (before.length > 0 && !before.endsWith('\n')) ? '\n' : '';
            const suffix = (after.length > 0 && !after.startsWith('\n')) ? '\n' : '';
            const insertion = prefix + normalizedText + suffix;

            el.value = before + insertion + after;
            el.selectionStart = el.selectionEnd = start + insertion.length;

            // Trigger change event so frameworks (e.g. Blazor) can observe the update
            el.dispatchEvent(new Event('input', { bubbles: true }));
            el.dispatchEvent(new Event('change', { bubbles: true }));

            if (typeof options?.onPaste === 'function') {
                options.onPaste(normalizedText);
            }
        });
    }

    /**
     * Parses raw text into all columns per row.
     * @param {string} text
     * @returns {string[][]}
     */
    function parseAllRows(text) {
        if (!text) return [];
        const normalized = text.replace(/\r\n/g, '\n').replace(/\r/g, '\n');
        const lines = normalized.split('\n');
        const rows = [];
        for (const line of lines) {
            if (!line.trim()) continue;
            if (line.includes('\t')) {
                rows.push(line.split('\t').map(c => c.trim()));
            } else if (line.includes(',')) {
                rows.push(parseCsvLine(line).map(c => c.trim()));
            } else {
                rows.push([line.trim()]);
            }
        }
        return rows;
    }

    /**
     * Reads a File object and parses it as CSV/TSV.
     * @param {File} file
     * @param {number} [maxRows=200]  Maximum number of DATA rows to return (header excluded)
     * @returns {Promise<{ headers: string[], rows: string[][], colCount: number }>}
     */
    function readFile(file, maxRows = 200) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onerror = () => reject(new Error('Failed to read file'));
            reader.onload = function (e) {
                const text = e.target.result;
                const allRows = parseAllRows(text);
                if (allRows.length === 0) { resolve({ headers: [], rows: [], colCount: 0 }); return; }

                const colCount = Math.max(...allRows.map(r => r.length));
                // Treat row 0 as headers when it looks like labels (or use generated names)
                const headerRow = allRows[0];
                const headers = Array.from({ length: colCount }, (_, i) => {
                    const val = (headerRow[i] ?? '').trim();
                    return val || `Column ${i + 1}`;
                });

                // Data rows = everything after the first row, capped at maxRows
                const rows = allRows.slice(1, maxRows + 1);
                resolve({ headers, rows, colCount });
            };
            reader.readAsText(file);
        });
    }

    /**
     * Populates a textarea with the values from a specific column index.
     * @param {HTMLTextAreaElement} ta
     * @param {string[][]} rows
     * @param {number} colIndex
     */
    function applyColumnToTextarea(ta, rows, colIndex) {
        const items = rows
            .map(r => (r[colIndex] ?? '').trim().replace(/^(["'])(.*)\1$/, '$2').trim())
            .filter(v => v.length > 0);
        ta.value = items.join('\n');
        ta.dispatchEvent(new Event('input', { bubbles: true }));
        ta.dispatchEvent(new Event('change', { bubbles: true }));
    }

    /**
     * Binds a complete CSV file-import flow to a file input + textarea + column picker.
     *
     * @param {{
     *   fileInputId: string,
     *   textareaId: string,
     *   columnPickerId: string,
     *   columnSelectId: string,
     *   maxRows?: number
     * }} config
     */
    function bindFileUpload(config) {
        const { fileInputId, textareaId, columnPickerId, columnSelectId, maxRows = 200 } = config;

        const fileInput = document.getElementById(fileInputId);
        const textarea  = document.getElementById(textareaId);
        const picker    = document.getElementById(columnPickerId);
        const colSelect = document.getElementById(columnSelectId);

        if (!fileInput || !textarea || !picker || !colSelect) return;
        if (fileInput.dataset.csvFileBound) return;
        fileInput.dataset.csvFileBound = '1';

        // Stored state while waiting for column selection
        let _pendingRows = [];

        function showPicker(headers) {
            colSelect.innerHTML = headers
                .map((h, i) => `<option value="${i}">${h}</option>`)
                .join('');
            picker.classList.remove('d-none');
            colSelect.focus();
        }

        function hidePicker() {
            picker.classList.add('d-none');
            _pendingRows = [];
            // Reset file input so same file can be re-selected
            fileInput.value = '';
        }

        // Apply selected column
        picker.querySelector('[data-csv-apply]').addEventListener('click', function () {
            const idx = parseInt(colSelect.value, 10);
            applyColumnToTextarea(textarea, _pendingRows, idx);
            hidePicker();
        });

        // Cancel
        picker.querySelector('[data-csv-cancel]').addEventListener('click', hidePicker);

        fileInput.addEventListener('change', async function () {
            const file = this.files[0];
            if (!file) return;

            try {
                const { headers, rows, colCount } = await readFile(file, maxRows);
                if (rows.length === 0) {
                    alert('The file appears to be empty or could not be parsed.');
                    fileInput.value = '';
                    return;
                }

                if (colCount <= 1) {
                    // Single column — apply immediately
                    applyColumnToTextarea(textarea, rows, 0);
                    fileInput.value = '';
                } else {
                    // Multi-column — show picker
                    _pendingRows = rows;
                    showPicker(headers);
                }
            } catch (err) {
                alert('Could not read the file. Please make sure it is a valid CSV/TSV file.');
                fileInput.value = '';
            }
        });
    }

    // Public API
    return {
        parseCsvLine,
        parseFirstColumn,
        normalize,
        needsNormalization,
        wireTextareaPaste,
        readFile,
        bindFileUpload
    };
})();
