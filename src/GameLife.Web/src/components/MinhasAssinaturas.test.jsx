import { fireEvent, render, screen, within } from '@testing-library/react';
import { vi } from 'vitest';
import MinhasAssinaturas from './MinhasAssinaturas.jsx';

afterEach(() => vi.unstubAllGlobals());

test.each([true, false])('salva a assinatura e mostra o resultado da API (%s)', async (ok) => {
  const servicos = ['Apple Arcade', 'Game Pass', 'Play Pass', 'PlayStation Plus'];
  const fetch = vi.fn()
    .mockResolvedValueOnce({ ok: true, json: async () => servicos.map((servico) => ({ servico, ativa: false })) })
    .mockResolvedValueOnce({ ok, json: async () => ok ? {} : { mensagem: 'Falha ao salvar.' } });
  vi.stubGlobal('fetch', fetch);
  render(<MinhasAssinaturas />);

  const linha = within(await screen.findByRole('form', { name: 'Game Pass' }));
  expect(screen.getAllByRole('form')).toHaveLength(4);
  const ativa = linha.getByLabelText('Ativa');
  const renovacao = linha.getByLabelText('Próxima renovação');
  fireEvent.click(ativa);
  fireEvent.change(renovacao, { target: { value: '2026-12-13' } });
  fireEvent.click(linha.getByRole('button', { name: 'Salvar' }));

  expect(fetch).toHaveBeenLastCalledWith('/api/minhas-assinaturas', expect.objectContaining({
    method: 'PUT',
    body: JSON.stringify({ servico: 'Game Pass', ativa: true, proximaRenovacao: '2026-12-13' })
  }));
  expect(await linha.findByRole(ok ? 'status' : 'alert')).toHaveTextContent(ok ? 'Salvo' : 'Falha ao salvar.');
  expect(ativa).toBeChecked();
  expect(renovacao).toHaveValue('2026-12-13');
});
