module.exports = {
  branches: [
    "main",
    "maintenance/net8"
  ],
  plugins: [
    [
      '@semantic-release/commit-analyzer',
      {
        preset: 'conventionalcommits',
        releaseRules: [
          {breaking: true, release: 'major'},
          {type: 'feat', release: 'minor'},
          {type: 'fix', release: 'patch'},
          {type: 'perf', release: 'patch'},
          {type: 'deps', release: false},
          {type: 'chore', release: false},
          {type: 'docs', release: false},
          {type: 'style', release: false},
          {type: 'test', release: false},
          {type: 'refactor', release: false},
          {type: 'ci', release: false},
          {type: 'build', release: false}
        ],
        parserOpts: {
          noteKeywords: [
            'BREAKING CHANGE',
            'BREAKING CHANGES'
          ]
        }
      }
    ],
    ['@semantic-release/release-notes-generator', {
      preset: 'conventionalcommits',
      presetConfig: {
        types: [
          {
            type: 'docs',
            section: '📚 Documentation',
            hidden: false
          },
          {
            type: 'fix',
            section: '🐛 Bug fixes',
            hidden: false
          },
          {
            type: 'feat',
            section: '✨ New features',
            hidden: false
          },
          {
            type: 'perf',
            section: '⚡ Performance improvement',
            hidden: false
          },
          {
            type: 'style',
            section: '💄 Code style adjustments',
            hidden: false
          },
          {
            type: 'test',
            section: '🧪 (Unit)test cases adjusted',
            hidden: false
          },
          {
            type: 'refactor',
            section: '♻️ Refactor',
            hidden: false
          },
          {
            type: 'chore',
            scope: 'deps',
            section: '⬆️ Dependency updates',
            hidden: false
          }
        ]
      },
      parserOpts: {
        noteKeywords: [
          'BREAKING CHANGE',
          'BREAKING CHANGES'
        ]
      }
    }],
    [
      "@semantic-release/exec",
      {
        "prepareCmd": "dotnet pack --configuration Release --output release-artifacts /p:Version=${nextRelease.version}"
      }
    ],
    [
      '@semantic-release/github',
      {
        assets: [
          'release-artifacts/*.nupkg',
          'release-artifacts/*.snupkg'
        ]
      }
    ]
  ]
}
