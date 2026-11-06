import { render, screen, fireEvent } from '@testing-library/react';
import { vi } from 'vitest';
import PainelRetrospectiva from './PainelRetrospectiva.jsx';

beforeEach(() => {
  global.fetch = vi.fn().mockResolvedValue({
    ok: true,
    json: async () => [
      { id: 1, titulo: 'Elden Ring', plataforma: 'PlayStation 5', status: 'NaoTerminado', tempoEmHoras: 71, conquistasObtidas: 30, conquistasPossiveis: 42, dataStatus: '2026-11-02' },
      { id: 2, titulo: 'Hades II', plataforma: 'STEAM', status: 'Zerado', tempoEmHoras: 38, conquistasObtidas: 30, conquistasPossiveis: 35, dataStatus: '2026-11-04' }
    ]
  });
});

afterEach(() => vi.restoreAllMocks());

test('renderiza o painel de retrospectiva com título e seletor de períodos', async () => {
  render(<PainelRetrospectiva />);
  expect(screen.getByRole('heading', { name: /retrospectiva de jogos/i })).toBeInTheDocument();
  expect(screen.getByText('Novembro/2026 (Atual)')).toBeInTheDocument();
  expect(screen.getByText('✨ Retrospectiva Anual 2026')).toBeInTheDocument();
});

test('alterna para o modo Wrapped Anual', async () => {
  render(<PainelRetrospectiva />);
  const botaoWrapped = screen.getByRole('button', { name: /wrapped anual/i });
  fireEvent.click(botaoWrapped);
  expect(await screen.findByText('Seu Ano no Mundo dos Games')).toBeInTheDocument();
});
