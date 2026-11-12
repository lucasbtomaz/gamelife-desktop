import { describe, expect, test } from 'vitest';
import { filtrarDesejos, formatarOrigens, normalizarBusca, obterOrigensMenores } from './wishlist.js';

describe('helpers da lista de desejos', () => {
  test('normaliza acentos e encontra o jogo pelo título', () => {
    const itens = [{ titulo: 'Pokémon Legends', precosAtuais: [], historicoPrecos: [] }];

    expect(normalizarBusca('  POKÉMON  ')).toBe('pokemon');
    expect(filtrarDesejos(itens, 'pokemon')).toEqual(itens);
  });

  test('encontra o item por uma loja presente somente no histórico', () => {
    const item = {
      titulo: 'Hades II',
      precosAtuais: [],
      historicoPrecos: [{ canalNome: 'Xbox Séries S', precoAtual: 79.9, disponivel: true }]
    };

    expect(filtrarDesejos([item], 'xbox series')).toEqual([item]);
  });

  test('ignora cotações indisponíveis ao obter as origens dos menores preços', () => {
    const item = {
      menorPrecoAtual: 40,
      menorPrecoHistorico: 30,
      precosAtuais: [
        { canalNome: 'STEAM', precoAtual: 40, disponivel: true },
        { canalNome: 'EPIC', precoAtual: 40, disponivel: false }
      ],
      historicoPrecos: [
        { canalNome: 'GOG', precoAtual: 30, disponivel: true },
        { canalNome: 'EPIC', precoAtual: 30, disponivel: false }
      ]
    };

    expect(obterOrigensMenores(item)).toEqual({ atual: ['STEAM'], historico: ['GOG'] });
  });

  test('deduplica e ordena lojas empatadas', () => {
    const item = {
      menorPrecoAtual: 50,
      menorPrecoHistorico: 20,
      precosAtuais: [
        { canalNome: 'Xbox Series S', precoAtual: 50, disponivel: true },
        { canalNome: 'Xbox One', precoAtual: 50, disponivel: true }
      ],
      historicoPrecos: [
        { canalNome: 'Xbox Series S', precoAtual: 20, disponivel: true },
        { canalNome: 'Xbox One', precoAtual: 20, disponivel: true },
        { canalNome: 'Xbox One', precoAtual: 20, disponivel: true }
      ]
    };

    expect(obterOrigensMenores(item)).toEqual({
      atual: ['Xbox One', 'Xbox Series S'],
      historico: ['Xbox One', 'Xbox Series S']
    });
    expect(formatarOrigens(['Xbox One', 'Xbox Series S'])).toBe('Xbox One +1');
  });
});
