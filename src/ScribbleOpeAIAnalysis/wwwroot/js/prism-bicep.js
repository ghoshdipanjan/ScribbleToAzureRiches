Prism.languages.bicep = {
    'comment': {
        pattern: /(^|[^\\])\/\/.*/,
        lookbehind: true
    },
    'string': {
        pattern: /'(?:\\.|[^\\'])*'|"(?:\\.|[^\\"])*"/,
        greedy: true
    },
    'keyword': /\b(?:param|var|resource|module|output|targetScope|existing|import)\b/,
    'decorator': {
        pattern: /@\w+/,
        alias: 'function'
    },
    'function': /\b[a-zA-Z_]\w*(?=\s*\()/,
    'boolean': /\b(?:true|false|null)\b/,
    'number': /\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b/,
    'operator': /[=!<>]=?|[-+*/%]|\b(?:and|or|not)\b/,
    'punctuation': /[{}[\];(),.:]/,
    'variable': {
        pattern: /\${(?:[^{}]|\{[^}]*\})*}/,
        inside: {
            'delimiter': {
                pattern: /^\${|}$/,
                alias: 'punctuation'
            },
            rest: null
        }
    }
};

Prism.languages.bicep.variable.inside.rest = Prism.languages.bicep;
